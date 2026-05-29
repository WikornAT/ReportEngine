using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.RenderLogs;
using Reporting.Domain.ReportDefinitions;
using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderPreview;

/// <summary>
/// Handles <see cref="RenderReportPreviewQuery"/>.
/// Delegates to <see cref="IReportRenderer"/> requesting <see cref="ReportOutputFormat.Html"/>
/// so that the raw merged HTML is returned without triggering a persisted execution.
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
    private readonly ILogger<RenderReportPreviewQueryHandler> _logger;

    public RenderReportPreviewQueryHandler(
        IReportingDbContext dbContext,
        IReportQueryExecutor queryExecutor,
        IReportRenderer renderer,
        ILogger<RenderReportPreviewQueryHandler> logger)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(
        RenderReportPreviewQuery request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .AsNoTracking()
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
            string dataJson = await _queryExecutor.ExecuteAsync(
                reportDefinitionId: definition.Id,
                parametersJson: request.ParametersJson,
                cancellationToken: cancellationToken);

            RenderedReport rendered = await _renderer.RenderAsync(
                reportDefinitionId: definition.Id,
                dataJson: dataJson,
                outputFormat: ReportOutputFormat.Html,
                cancellationToken: cancellationToken);

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
}
