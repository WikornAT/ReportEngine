using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Text.Json;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.RenderLogs;
using Reporting.Domain.ReportDefinitions;
using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderPdf;

/// <summary>
/// Handles <see cref="RenderReportPdfQuery"/>.
/// Validates parameters, delegates to <see cref="IReportRenderer"/> for PDF output,
/// and persists a <see cref="RenderLog"/> with per-phase timings.
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
    private readonly IParameterValidator _parameterValidator;
    private readonly ILogger<RenderReportPdfQueryHandler> _logger;

    public RenderReportPdfQueryHandler(
        IReportingDbContext dbContext,
        IReportQueryExecutor queryExecutor,
        IReportRenderer renderer,
        IParameterValidator parameterValidator,
        ILogger<RenderReportPdfQueryHandler> logger)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _renderer = renderer;
        _parameterValidator = parameterValidator;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(
        RenderReportPdfQuery request,
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

        // Validate and normalise parameters
        ParameterValidationResult validation = _parameterValidator.Validate(
            definition.Parameters,
            request.ParametersJson);

        if (!validation.IsValid)
        {
            return AppError.Validation(string.Join(" | ", validation.Errors));
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
            DataSourceExecutionResult executionResult = await _queryExecutor.ExecuteAsync(
                reportDefinitionId: definition.Id,
                parametersJson: request.ParametersJson,
                cancellationToken: cancellationToken);

            // Merge user-supplied parameters into the data JSON so the renderer
            // can bind {{ params.* }} placeholders in the template.
            string mergedDataJson = MergeParametersIntoDataJson(
                request.ParametersJson, executionResult.DataJson);

            RenderedReport rendered = await _renderer.RenderAsync(
                reportDefinitionId: definition.Id,
                dataJson: mergedDataJson,
                outputFormat: ReportOutputFormat.Pdf,
                cancellationToken: cancellationToken);

            RenderPhaseTimings? timings = rendered.PhaseTimings;
            log.RecordPhaseTimings(
                parametersJson: request.ParametersJson,
                dataSourceExecutionMs: executionResult.DataSourceExecutionMs,
                templateBindingMs: timings?.TemplateBindingMs ?? 0,
                renderMs: timings?.RenderMs ?? 0);

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

    /// <summary>
    /// Merges user-supplied <paramref name="parametersJson"/> with data-source results
    /// (<paramref name="dataJson"/>) into the payload consumed by the renderer.
    /// <para>
    /// <b>Single-page (object)</b>: both JSONs are merged into one flat object.
    /// Parameter keys win on collision. The renderer binds <c>{{ params.* }}</c>.
    /// </para>
    /// <para>
    /// <b>Multi-page (array)</b>: <paramref name="parametersJson"/> must be a JSON array.
    /// Each element is merged with <paramref name="dataJson"/> individually and the result
    /// is returned as a JSON array; <see cref="IReportRenderer"/> will iterate the array
    /// and produce one PDF page per element.
    /// </para>
    /// </summary>
    private static string MergeParametersIntoDataJson(string parametersJson, string dataJson)
    {
        // Build base dict from data-source results
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

        // ── Array: multi-page render ──────────────────────────────────────────
        if (paramDoc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var pages = new List<Dictionary<string, JsonElement>>();
            foreach (JsonElement element in paramDoc.RootElement.EnumerateArray())
            {
                var page = new Dictionary<string, JsonElement>(baseData, StringComparer.OrdinalIgnoreCase);
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        page[prop.Name] = prop.Value.Clone();
                    }
                }
                pages.Add(page);
            }
            return JsonSerializer.Serialize(pages);
        }

        // ── Object: single-page render ────────────────────────────────────────
        var merged = new Dictionary<string, JsonElement>(baseData, StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty prop in paramDoc.RootElement.EnumerateObject())
        {
            merged[prop.Name] = prop.Value.Clone();
        }
        return JsonSerializer.Serialize(merged);
    }
}
