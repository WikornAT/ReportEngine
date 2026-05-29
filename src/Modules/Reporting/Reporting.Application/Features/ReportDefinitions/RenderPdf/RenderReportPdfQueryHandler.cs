using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.RenderLogs;
using Reporting.Domain.ReportDefinitions;
using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderPdf;

/// <summary>
/// Handles <see cref="RenderReportPdfQuery"/>.
/// Delegates to <see cref="IReportRenderer"/> requesting <see cref="ReportOutputFormat.Pdf"/>
/// and returns the raw PDF bytes without persisting a <c>ReportExecution</c>.
/// </summary>
internal sealed class RenderReportPdfQueryHandler
    : IRequestHandler<RenderReportPdfQuery, Result<byte[]>>
{
    private static readonly Action<ILogger, Guid, Exception?> _logRender =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(22, "ReportPdfRenderRequested"),
            "Inline PDF render requested for ReportDefinition {ReportDefinitionId}");

    private static readonly Action<ILogger, Guid, long, int, Exception?> _logRenderDone =
        LoggerMessage.Define<Guid, long, int>(
            LogLevel.Information,
            new EventId(23, "ReportPdfRenderCompleted"),
            "Inline PDF render for ReportDefinition {ReportDefinitionId} completed in {ElapsedMs}ms, size={SizeBytes}B");

    private readonly IReportingDbContext _dbContext;
    private readonly IReportQueryExecutor _queryExecutor;
    private readonly IReportRenderer _renderer;
    private readonly ILogger<RenderReportPdfQueryHandler> _logger;

    public RenderReportPdfQueryHandler(
        IReportingDbContext dbContext,
        IReportQueryExecutor queryExecutor,
        IReportRenderer renderer,
        ILogger<RenderReportPdfQueryHandler> logger)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(
        RenderReportPdfQuery request,
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
                "Call AssignTemplate before rendering.");
        }

        _logRender(_logger, request.ReportDefinitionId, null);
        long started = Environment.TickCount64;

        RenderLog log = RenderLog.Start(
            reportDefinitionId: definition.Id,
            templateId: definition.TemplateId,
            format: ReportOutputFormat.Pdf,
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
                outputFormat: ReportOutputFormat.Pdf,
                cancellationToken: cancellationToken);

            log.Succeed(rendered.Content.Length);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logRenderDone(
                _logger,
                request.ReportDefinitionId,
                Environment.TickCount64 - started,
                rendered.Content.Length,
                null);

            return rendered.Content;
        }
        catch (Exception ex)
        {
            log.Fail(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
