using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Text.Json;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.RenderLogs;
using Reporting.Domain.ReportDefinitions;
using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderPreview;

/// <summary>
/// Handles <see cref="RenderReportPreviewQuery"/>.
/// Validates parameters, delegates to <see cref="IReportRenderer"/> for HTML output,
/// and persists a <see cref="RenderLog"/> with per-phase timings.
/// </summary>
internal sealed class RenderReportPreviewQueryHandler
    : IRequestHandler<RenderReportPreviewQuery, Result<string>>
{
    private static readonly Action<ILogger, Guid, Exception?> _logPreview =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(20, "ReportPreviewRequested"),
            "HTML preview requested for ReportDefinition {ReportDefinitionId}");

    private static readonly Action<ILogger, Guid, long, Exception?> _logPreviewDone =
        LoggerMessage.Define<Guid, long>(
            LogLevel.Information,
            new EventId(21, "ReportPreviewCompleted"),
            "HTML preview for ReportDefinition {ReportDefinitionId} completed in {ElapsedMs}ms");

    private readonly IReportingDbContext _dbContext;
    private readonly IReportQueryExecutor _queryExecutor;
    private readonly IReportRenderer _renderer;
    private readonly IParameterValidator _parameterValidator;
    private readonly ILogger<RenderReportPreviewQueryHandler> _logger;

    public RenderReportPreviewQueryHandler(
        IReportingDbContext dbContext,
        IReportQueryExecutor queryExecutor,
        IReportRenderer renderer,
        IParameterValidator parameterValidator,
        ILogger<RenderReportPreviewQueryHandler> logger)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _renderer = renderer;
        _parameterValidator = parameterValidator;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(
        RenderReportPreviewQuery request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(d => d.Parameters)
            .FirstOrDefaultAsync(d => d.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        if (definition.TemplateId is null)
        {
            return AppError.DomainViolation(
                $"ReportDefinition '{definition.Name}' has no template assigned. " +
                "Call AssignTemplate before requesting a preview.");
        }

        // Validate and normalise parameters
        ParameterValidationResult validation = _parameterValidator.Validate(
            definition.Parameters,
            request.ParametersJson);

        if (!validation.IsValid)
        {
            return AppError.Validation(string.Join(" | ", validation.Errors));
        }

        _logPreview(_logger, request.ReportDefinitionId, null);
        long started = Environment.TickCount64;

        RenderLog log = RenderLog.Start(
            reportDefinitionId: definition.Id,
            templateId: definition.TemplateId,
            format: ReportOutputFormat.Html,
            triggeredBy: request.TriggeredBy);
        _dbContext.RenderLogs.Add(log);

        try
        {
            DataSourceExecutionResult executionResult = await _queryExecutor.ExecuteAsync(
                reportDefinitionId: definition.Id,
                parametersJson: request.ParametersJson,
                cancellationToken: cancellationToken);

            string mergedDataJson = MergeParametersIntoDataJson(
                request.ParametersJson, executionResult.DataJson);

            RenderedReport rendered = await _renderer.RenderAsync(
                reportDefinitionId: definition.Id,
                dataJson: mergedDataJson,
                outputFormat: ReportOutputFormat.Html,
                cancellationToken: cancellationToken);

            RenderPhaseTimings? timings = rendered.PhaseTimings;
            log.RecordPhaseTimings(
                parametersJson: request.ParametersJson,
                dataSourceExecutionMs: executionResult.DataSourceExecutionMs,
                templateBindingMs: timings?.TemplateBindingMs ?? 0,
                renderMs: timings?.RenderMs ?? 0);

            log.Succeed(rendered.Content.Length);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logPreviewDone(_logger, request.ReportDefinitionId, Environment.TickCount64 - started, null);

            return System.Text.Encoding.UTF8.GetString(rendered.Content);
        }
        catch (Exception ex)
        {
            log.Fail(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Merges user-supplied parameters with data-source results.
    /// For <b>array</b> input (multi-page), only the <b>first element</b> is used for preview.
    /// </summary>
    private static string MergeParametersIntoDataJson(string parametersJson, string dataJson)
    {
        var baseData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(dataJson) && dataJson.Trim() != "{}")
        {
            using JsonDocument dataDoc = JsonDocument.Parse(dataJson);
            foreach (JsonProperty prop in dataDoc.RootElement.EnumerateObject())
            {
                baseData[prop.Name] = prop.Value.Clone();
            }
        }

        if (string.IsNullOrWhiteSpace(parametersJson) || parametersJson.Trim() == "{}")
        {
            return JsonSerializer.Serialize(baseData);
        }

        using JsonDocument paramDoc = JsonDocument.Parse(parametersJson);

        // Array → use first element only for preview
        JsonElement paramRoot = paramDoc.RootElement.ValueKind == JsonValueKind.Array
            && paramDoc.RootElement.GetArrayLength() > 0
            ? paramDoc.RootElement[0]
            : paramDoc.RootElement;

        var merged = new Dictionary<string, JsonElement>(baseData, StringComparer.OrdinalIgnoreCase);
        if (paramRoot.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in paramRoot.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }
        }
        return JsonSerializer.Serialize(merged);
    }
}
