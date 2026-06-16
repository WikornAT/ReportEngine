using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Reporting.Domain.ReportDefinitions;
using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderBatch;

/// <summary>
/// Handles <see cref="RenderBatchReportCommand"/>.
/// Loads and validates the report definition, then delegates rendering and
/// execution logging to <see cref="IBatchReportOrchestrator"/>.
/// </summary>
internal sealed class RenderBatchReportCommandHandler
    : IRequestHandler<RenderBatchReportCommand, Result<BatchRenderResult>>
{
    private static readonly Action<ILogger, Guid, string, int, Exception?> _logBatch =
        LoggerMessage.Define<Guid, string, int>(
            LogLevel.Information,
            new EventId(30, "BatchRenderRequested"),
            "Batch render requested for ReportDefinition {ReportDefinitionId}, mode={Mode}, items={ItemCount}");

    private static readonly Action<ILogger, Guid, long, Exception?> _logBatchDone =
        LoggerMessage.Define<Guid, long>(
            LogLevel.Information,
            new EventId(31, "BatchRenderCompleted"),
            "Batch render for ReportDefinition {ReportDefinitionId} completed in {ElapsedMs}ms");

    private readonly IReportingDbContext _dbContext;
    private readonly IParameterValidator _parameterValidator;
    private readonly IBatchReportOrchestrator _orchestrator;
    private readonly ILogger<RenderBatchReportCommandHandler> _logger;

    public RenderBatchReportCommandHandler(
        IReportingDbContext dbContext,
        IParameterValidator parameterValidator,
        IBatchReportOrchestrator orchestrator,
        ILogger<RenderBatchReportCommandHandler> logger)
    {
        _dbContext = dbContext;
        _parameterValidator = parameterValidator;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<Result<BatchRenderResult>> Handle(
        RenderBatchReportCommand request,
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
                "Call AssignTemplate before rendering.");
        }

        // Validate every parameter set against the declared parameters
        var validationErrors = new List<string>();
        for (int i = 0; i < request.ParametersJsonItems.Count; i++)
        {
            ParameterValidationResult validation = _parameterValidator.Validate(
                definition.Parameters,
                request.ParametersJsonItems[i]);

            if (!validation.IsValid)
            {
                validationErrors.AddRange(
                    validation.Errors.Select(e => $"Item[{i}]: {e}"));
            }
        }

        if (validationErrors.Count > 0)
        {
            return AppError.Validation(string.Join(" | ", validationErrors));
        }

        _logBatch(
            _logger,
            request.ReportDefinitionId,
            request.RenderMode.ToString(),
            request.ParametersJsonItems.Count,
            null);

        long started = Environment.TickCount64;
        Guid batchExecutionId = Guid.NewGuid();

        BatchRenderResult result = await _orchestrator.RenderBatchAsync(
            reportDefinitionId: definition.Id,
            reportName: definition.Name,
            mode: request.RenderMode,
            parametersJsonItems: request.ParametersJsonItems,
            batchExecutionId: batchExecutionId,
            outputFileNamePattern: request.OutputFileNamePattern,
            continueOnError: request.ContinueOnError,
            triggeredBy: request.TriggeredBy,
            cancellationToken: cancellationToken);

        _logBatchDone(_logger, request.ReportDefinitionId, Environment.TickCount64 - started, null);

        return result;
    }
}
