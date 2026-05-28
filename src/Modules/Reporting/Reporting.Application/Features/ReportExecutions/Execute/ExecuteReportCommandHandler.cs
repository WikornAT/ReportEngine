using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;
using Reporting.Domain.ReportExecutions;

namespace Reporting.Application.Features.ReportExecutions.Execute;

/// <summary>
/// Handles <see cref="ExecuteReportCommand"/>.
/// <para>
/// Full execution flow:
/// <list type="number">
///   <item>Load and validate the <see cref="ReportDefinition"/> (must be Active).</item>
///   <item>Validate that all required parameters are present in <c>ParametersJson</c>.</item>
///   <item>Create a <see cref="ReportExecution"/> in Queued state and persist.</item>
///   <item>Transition to Running and call <see cref="IReportQueryExecutor"/>.</item>
///   <item>For each requested format, call <see cref="IReportRenderer"/> and
///         <see cref="IReportOutputStorage"/>, then attach <see cref="ReportOutputFile"/>.</item>
///   <item>Mark execution Completed (or Failed on any exception) and persist final state.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class ExecuteReportCommandHandler
    : IRequestHandler<ExecuteReportCommand, Result<ReportExecutionDto>>
{
    private static readonly Action<ILogger, Guid, string, string, Exception?> _logQueued =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Information,
            new EventId(1, "ReportExecutionQueued"),
            "ReportExecution {ExecutionId} queued for report '{ReportName}' by {User}");

    private static readonly Action<ILogger, Guid, long, Exception?> _logCompleted =
        LoggerMessage.Define<Guid, long>(
            LogLevel.Information,
            new EventId(2, "ReportExecutionCompleted"),
            "ReportExecution {ExecutionId} completed in {DurationMs}ms");

    private static readonly Action<ILogger, Guid, string, Exception?> _logFailed =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Error,
            new EventId(3, "ReportExecutionFailed"),
            "ReportExecution {ExecutionId} failed: {Message}");

    private static readonly Action<ILogger, Guid, Exception?> _logPersistFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(4, "ReportExecutionPersistFailed"),
            "Failed to persist Failed state for ReportExecution {ExecutionId}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IReportQueryExecutor _queryExecutor;
    private readonly IReportRenderer _renderer;
    private readonly IReportOutputStorage _storage;
    private readonly ILogger<ExecuteReportCommandHandler> _logger;

    public ExecuteReportCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        IReportQueryExecutor queryExecutor,
        IReportRenderer renderer,
        IReportOutputStorage storage,
        ILogger<ExecuteReportCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _queryExecutor = queryExecutor;
        _renderer = renderer;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<ReportExecutionDto>> Handle(
        ExecuteReportCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Load report definition ─────────────────────────────────────────
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Parameters)
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        // ── 2. Ensure report is Active ────────────────────────────────────────
        if (definition.Status != ReportStatus.Active)
        {
            return AppError.DomainViolation(
                $"Report '{definition.Name}' cannot be executed because its status is '{definition.Status}'. " +
                "Only Active reports can be executed.");
        }

        // ── 3. Validate required parameters are supplied ──────────────────────
        Result<bool> paramValidation = ValidateRequiredParameters(definition, request.ParametersJson);
        if (paramValidation.IsFailure)
        {
            return paramValidation.Error;
        }

        // ── 4. Create execution in Queued state and persist ───────────────────
        ReportExecution execution = ReportExecution.Queue(
            reportDefinitionId: definition.Id,
            reportName: definition.Name,
            parametersJson: request.ParametersJson,
            requestedFormats: request.RequestedFormats,
            triggeredBy: _currentUser.UserId,
            correlationId: request.CorrelationId);

        _dbContext.ReportExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logQueued(_logger, execution.Id, definition.Name, _currentUser.UserId, null);

        // ── 5. Transition to Running ──────────────────────────────────────────
        execution.Start();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ── 6. Execute queries and render ─────────────────────────────────────
        try
        {
            string dataJson = await _queryExecutor.ExecuteAsync(
                reportDefinitionId: definition.Id,
                parametersJson: request.ParametersJson,
                cancellationToken: cancellationToken);

            foreach (ReportOutputFormat format in request.RequestedFormats)
            {
                RenderedReport rendered = await _renderer.RenderAsync(
                    reportDefinitionId: definition.Id,
                    dataJson: dataJson,
                    outputFormat: format,
                    cancellationToken: cancellationToken);

                string storagePath = await _storage.SaveAsync(
                    executionId: execution.Id,
                    fileName: rendered.FileName,
                    content: rendered.Content,
                    cancellationToken: cancellationToken);

                execution.AddOutputFile(
                    outputFormat: format,
                    fileName: rendered.FileName,
                    storagePath: storagePath,
                    contentType: rendered.ContentType,
                    fileSizeBytes: rendered.Content.LongLength);
            }

            // ── 7. Mark Completed ─────────────────────────────────────────────
            execution.Complete();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logCompleted(_logger, execution.Id, execution.DurationMs ?? 0L, null);

            return execution.ToDto();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ── 8. Mark Failed — best-effort persist ──────────────────────────
            _logFailed(_logger, execution.Id, ex.Message, ex);

            try
            {
                execution.Fail(ex.Message);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception persistEx)
            {
                _logPersistFailed(_logger, execution.Id, persistEx);
            }

            return AppError.Unexpected(ex.Message);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Checks that every required parameter declared on the definition has a corresponding
    /// key in the supplied <paramref name="parametersJson"/> object.
    /// This is a structural check only — type coercion is done by the query executor.
    /// </summary>
    private static Result<bool> ValidateRequiredParameters(
        ReportDefinition definition,
        string parametersJson)
    {
        IEnumerable<ReportParameter> requiredParams = definition.Parameters
            .Where(p => p.IsRequired);

        // Parse keys from the JSON object using System.Text.Json
        System.Text.Json.JsonElement root;
        try
        {
            root = System.Text.Json.JsonDocument.Parse(parametersJson).RootElement;
        }
        catch (System.Text.Json.JsonException)
        {
            return AppError.Validation("ParametersJson is not a valid JSON object.");
        }

        if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return AppError.Validation("ParametersJson must be a JSON object (e.g., {\"StartDate\":\"2025-01-01\"}).");
        }

        List<string> missing = [];

        foreach (ReportParameter param in requiredParams)
        {
            bool supplied =
                root.TryGetProperty(param.Name, out System.Text.Json.JsonElement val) &&
                val.ValueKind != System.Text.Json.JsonValueKind.Null;

            if (!supplied)
            {
                missing.Add(param.Name);
            }
        }

        if (missing.Count > 0)
        {
            return AppError.Validation(
                $"The following required parameters are missing or null: {string.Join(", ", missing)}.");
        }

        return true;
    }
}
