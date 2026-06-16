using System.IO.Compression;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportExecutions;
using ReportEngine.SharedKernel;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IBatchReportOrchestrator"/> by delegating to
/// <see cref="IReportRenderer"/> and <see cref="IReportQueryExecutor"/> per item.
/// <para>
/// <b>MergePdf / PreviewHtml</b>: Executes data sources per item, enriches each item's
/// dataJson with batch system context (<c>_sys_batch_*</c>), builds a JSON array,
/// and calls the renderer once so the existing multi-page composition path in
/// <see cref="HtmlReportRenderer"/> handles page-break insertion.
/// </para>
/// <para>
/// <b>ZipPdf</b>: Renders each item to a separate PDF and packages them into a ZIP.
/// </para>
/// <para>
/// <b>Single</b>: Delegates to a single-item render for the one parameter object.
/// </para>
/// </summary>
internal sealed class BatchReportOrchestrator : IBatchReportOrchestrator
{
    private static readonly Action<ILogger, int, string, Exception?> _logStart =
        LoggerMessage.Define<int, string>(
            LogLevel.Information,
            new EventId(40, "BatchOrchestrationStarted"),
            "Batch orchestration started: {ItemCount} items, mode={Mode}");

    private static readonly Action<ILogger, int, int, Exception?> _logItemStart =
        LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(41, "BatchItemRenderStarted"),
            "Rendering batch item {Index}/{Total}");

    private static readonly Action<ILogger, int, long, Exception?> _logItemDone =
        LoggerMessage.Define<int, long>(
            LogLevel.Debug,
            new EventId(42, "BatchItemRenderCompleted"),
            "Batch item {Index} rendered in {ElapsedMs}ms");

    private static readonly Action<ILogger, int, string, Exception?> _logItemError =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(43, "BatchItemRenderFailed"),
            "Batch item {Index} failed: {Error}");

    private readonly IReportingDbContext _dbContext;
    private readonly IReportQueryExecutor _queryExecutor;
    private readonly IReportRenderer _renderer;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<BatchReportOrchestrator> _logger;

    public BatchReportOrchestrator(
        IReportingDbContext dbContext,
        IReportQueryExecutor queryExecutor,
        IReportRenderer renderer,
        IDateTimeProvider dateTime,
        ILogger<BatchReportOrchestrator> logger)
    {
        _dbContext     = dbContext;
        _queryExecutor = queryExecutor;
        _renderer      = renderer;
        _dateTime      = dateTime;
        _logger        = logger;
    }

    public async Task<BatchRenderResult> RenderBatchAsync(
        Guid reportDefinitionId,
        string reportName,
        RenderMode mode,
        IReadOnlyList<string> parametersJsonItems,
        Guid batchExecutionId,
        string? outputFileNamePattern,
        bool continueOnError,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        _logStart(_logger, parametersJsonItems.Count, mode.ToString(), null);

        int total = parametersJsonItems.Count;
        string effectivePattern = outputFileNamePattern ?? "{reportName}_{index}";
        DateTimeOffset now = _dateTime.UtcNow;

        // ── Create parent execution ───────────────────────────────────────────
        ReportExecution parentExecution = CreateAndStartExecution(
            reportDefinitionId, reportName, "{}", mode, batchExecutionId,
            parentExecutionId: null, batchItemIndex: null, outputFileName: null,
            triggeredBy, requestedFormat: FormatForMode(mode), now: now);

        _dbContext.ReportExecutions.Add(parentExecution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            BatchRenderResult result = mode switch
            {
                RenderMode.Single      => await RenderSingleAsync(reportDefinitionId, reportName, parametersJsonItems, batchExecutionId, effectivePattern, triggeredBy, parentExecution, cancellationToken),
                RenderMode.MergePdf    => await RenderMergedAsync(reportDefinitionId, reportName, parametersJsonItems, batchExecutionId, effectivePattern, triggeredBy, parentExecution, ReportOutputFormat.Pdf, cancellationToken),
                RenderMode.PreviewHtml => await RenderMergedAsync(reportDefinitionId, reportName, parametersJsonItems, batchExecutionId, effectivePattern, triggeredBy, parentExecution, ReportOutputFormat.Html, cancellationToken),
                RenderMode.ZipPdf      => await RenderZipAsync(reportDefinitionId, reportName, parametersJsonItems, batchExecutionId, effectivePattern, continueOnError, triggeredBy, parentExecution, cancellationToken),
                _                      => throw new NotSupportedException($"RenderMode '{mode}' is not supported.")
            };

            parentExecution.Complete(_dateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch
        {
            parentExecution.Fail("Batch rendering failed. See child executions for details.", _dateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // ── Single ────────────────────────────────────────────────────────────────

    private async Task<BatchRenderResult> RenderSingleAsync(
        Guid reportDefinitionId,
        string reportName,
        IReadOnlyList<string> parametersJsonItems,
        Guid batchExecutionId,
        string pattern,
        string triggeredBy,
        ReportExecution parentExecution,
        CancellationToken ct)
    {
        string outputFileName = ResolveFileName(pattern, reportName, 0, batchExecutionId, "pdf");

        ReportExecution childExecution = CreateAndStartExecution(
            reportDefinitionId, reportName, parametersJsonItems[0], RenderMode.Single,
            batchExecutionId, parentExecution.Id, 0, outputFileName, triggeredBy,
            ReportOutputFormat.Pdf, now: _dateTime.UtcNow);

        _dbContext.ReportExecutions.Add(childExecution);
        await _dbContext.SaveChangesAsync(ct);

        long itemStart = Environment.TickCount64;
        try
        {
            _logItemStart(_logger, 0, 1, null);

            DataSourceExecutionResult executionResult = await _queryExecutor.ExecuteAsync(
                reportDefinitionId, parametersJsonItems[0], ct);

            string mergedJson = EnrichItemJson(parametersJsonItems[0], executionResult.DataJson, 0, 1, batchExecutionId);

            RenderedReport rendered = await _renderer.RenderAsync(
                reportDefinitionId, mergedJson, ReportOutputFormat.Pdf, ct);

            long durationMs = Environment.TickCount64 - itemStart;
            _logItemDone(_logger, 0, durationMs, null);

            childExecution.Complete(_dateTime.UtcNow);
            await _dbContext.SaveChangesAsync(ct);

            var item = new BatchItemResult(0, true, null, durationMs, outputFileName);

            return new BatchRenderResult(
                RenderMode.Single,
                rendered.Content,
                "application/pdf",
                outputFileName,
                batchExecutionId,
                [item]);
        }
        catch (Exception ex)
        {
            long durationMs = Environment.TickCount64 - itemStart;
            _logItemError(_logger, 0, ex.Message, null);
            childExecution.Fail(ex.Message, _dateTime.UtcNow);
            await _dbContext.SaveChangesAsync(ct);
            throw;
        }
    }

    // ── MergePdf / PreviewHtml ────────────────────────────────────────────────

    private async Task<BatchRenderResult> RenderMergedAsync(
        Guid reportDefinitionId,
        string reportName,
        IReadOnlyList<string> parametersJsonItems,
        Guid batchExecutionId,
        string pattern,
        string triggeredBy,
        ReportExecution parentExecution,
        ReportOutputFormat outputFormat,
        CancellationToken ct)
    {
        bool isPdf = outputFormat == ReportOutputFormat.Pdf;
        RenderMode mode = isPdf ? RenderMode.MergePdf : RenderMode.PreviewHtml;
        string ext = isPdf ? "pdf" : "html";
        string contentType = isPdf ? "application/pdf" : "text/html; charset=utf-8";
        string mergedFileName = ResolveFileName(pattern, reportName, 0, batchExecutionId, ext)
            .Replace("_0.", ".");

        int total = parametersJsonItems.Count;

        // Execute data sources per item and build enriched JSON array
        var enrichedItems = new List<string>(total);
        var childExecutions = new List<ReportExecution>(total);
        var itemResults = new List<BatchItemResult>(total);

        for (int i = 0; i < total; i++)
        {
            string itemFileName = ResolveFileName(pattern, reportName, i, batchExecutionId, ext);
            ReportExecution child = CreateAndStartExecution(
                reportDefinitionId, reportName, parametersJsonItems[i], mode,
                batchExecutionId, parentExecution.Id, i, itemFileName, triggeredBy,
                outputFormat, now: _dateTime.UtcNow);
            childExecutions.Add(child);
            _dbContext.ReportExecutions.Add(child);
        }

        await _dbContext.SaveChangesAsync(ct);

        for (int i = 0; i < total; i++)
        {
            _logItemStart(_logger, i, total, null);
            long itemStart = Environment.TickCount64;

            DataSourceExecutionResult executionResult = await _queryExecutor.ExecuteAsync(
                reportDefinitionId, parametersJsonItems[i], ct);

            enrichedItems.Add(EnrichItemJson(parametersJsonItems[i], executionResult.DataJson, i, total, batchExecutionId));

            long durationMs = Environment.TickCount64 - itemStart;
            _logItemDone(_logger, i, durationMs, null);
            itemResults.Add(new BatchItemResult(i, true, null, durationMs, childExecutions[i].OutputFileName));
        }

        // Build JSON array and call renderer once (HtmlReportRenderer handles composition)
        string arrayJson = BuildArrayJson(enrichedItems);

        RenderedReport rendered = await _renderer.RenderAsync(
            reportDefinitionId, arrayJson, outputFormat, ct);

        DateTimeOffset completedAt = _dateTime.UtcNow;
        foreach (ReportExecution child in childExecutions)
        {
            child.Complete(completedAt);
        }

        await _dbContext.SaveChangesAsync(ct);

        return new BatchRenderResult(
            mode,
            rendered.Content,
            contentType,
            mergedFileName,
            batchExecutionId,
            itemResults);
    }

    // ── ZipPdf ────────────────────────────────────────────────────────────────

    private async Task<BatchRenderResult> RenderZipAsync(
        Guid reportDefinitionId,
        string reportName,
        IReadOnlyList<string> parametersJsonItems,
        Guid batchExecutionId,
        string pattern,
        bool continueOnError,
        string triggeredBy,
        ReportExecution parentExecution,
        CancellationToken ct)
    {
        int total = parametersJsonItems.Count;
        string zipFileName = $"{reportName}_{batchExecutionId:N}.zip";

        var childExecutions = new List<ReportExecution>(total);
        var itemResults = new List<BatchItemResult>(total);

        for (int i = 0; i < total; i++)
        {
            string itemFileName = ResolveFileName(pattern, reportName, i, batchExecutionId, "pdf");
            ReportExecution child = CreateAndStartExecution(
                reportDefinitionId, reportName, parametersJsonItems[i], RenderMode.ZipPdf,
                batchExecutionId, parentExecution.Id, i, itemFileName, triggeredBy,
                ReportOutputFormat.Pdf, now: _dateTime.UtcNow);
            childExecutions.Add(child);
            _dbContext.ReportExecutions.Add(child);
        }

        await _dbContext.SaveChangesAsync(ct);

        var pdfEntries = new List<(string FileName, byte[] Bytes)>(total);

        for (int i = 0; i < total; i++)
        {
            _logItemStart(_logger, i, total, null);
            long itemStart = Environment.TickCount64;
            string itemFileName = childExecutions[i].OutputFileName!;

            try
            {
                DataSourceExecutionResult executionResult = await _queryExecutor.ExecuteAsync(
                    reportDefinitionId, parametersJsonItems[i], ct);

                string mergedJson = EnrichItemJson(parametersJsonItems[i], executionResult.DataJson, i, total, batchExecutionId);

                RenderedReport rendered = await _renderer.RenderAsync(
                    reportDefinitionId, mergedJson, ReportOutputFormat.Pdf, ct);

                pdfEntries.Add((itemFileName, rendered.Content));

                long durationMs = Environment.TickCount64 - itemStart;
                _logItemDone(_logger, i, durationMs, null);

                childExecutions[i].Complete(_dateTime.UtcNow);
                itemResults.Add(new BatchItemResult(i, true, null, durationMs, itemFileName));
            }
            catch (Exception ex)
            {
                long durationMs = Environment.TickCount64 - itemStart;
                _logItemError(_logger, i, ex.Message, null);
                childExecutions[i].Fail(ex.Message, _dateTime.UtcNow);
                itemResults.Add(new BatchItemResult(i, false, ex.Message, durationMs, itemFileName));

                if (!continueOnError)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    throw;
                }
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        byte[] zipBytes = BuildZip(pdfEntries);

        return new BatchRenderResult(
            RenderMode.ZipPdf,
            zipBytes,
            "application/zip",
            zipFileName,
            batchExecutionId,
            itemResults);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReportExecution CreateAndStartExecution(
        Guid reportDefinitionId,
        string reportName,
        string parametersJson,
        RenderMode mode,
        Guid batchExecutionId,
        Guid? parentExecutionId,
        int? batchItemIndex,
        string? outputFileName,
        string triggeredBy,
        ReportOutputFormat requestedFormat,
        DateTimeOffset now)
    {
        ReportExecution execution = ReportExecution.QueueBatch(
            reportDefinitionId: reportDefinitionId,
            reportName:         reportName,
            parametersJson:     parametersJson,
            requestedFormats:   [requestedFormat],
            triggeredBy:        triggeredBy,
            now:                now,
            renderMode:         mode,
            batchExecutionId:   batchExecutionId,
            parentExecutionId:  parentExecutionId,
            batchItemIndex:     batchItemIndex,
            outputFileName:     outputFileName);

        execution.Start(now);
        return execution;
    }

    /// <summary>
    /// Merges the per-item <paramref name="parametersJson"/> with data source
    /// <paramref name="dataJson"/> and injects <c>_sys_batch_*</c> keys so the renderer
    /// can populate <c>system.batch_index</c>, <c>system.batch_total</c>, and
    /// <c>system.batch_execution_id</c> in the template.
    /// </summary>
    private static string EnrichItemJson(
        string parametersJson,
        string dataJson,
        int index,
        int total,
        Guid batchExecutionId)
    {
        var merged = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        // Start with data source results
        if (!string.IsNullOrWhiteSpace(dataJson) && dataJson.Trim() != "{}")
        {
            using JsonDocument dataDoc = JsonDocument.Parse(dataJson);
            foreach (JsonProperty prop in dataDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }
        }

        // Overlay user parameters
        if (!string.IsNullOrWhiteSpace(parametersJson) && parametersJson.Trim() != "{}")
        {
            using JsonDocument paramDoc = JsonDocument.Parse(parametersJson);
            if (paramDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty prop in paramDoc.RootElement.EnumerateObject())
                {
                    merged[prop.Name] = prop.Value.Clone();
                }
            }
        }

        // Inject batch system context
        using JsonDocument indexDoc = JsonDocument.Parse(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        using JsonDocument totalDoc = JsonDocument.Parse(total.ToString(System.Globalization.CultureInfo.InvariantCulture));
        using JsonDocument execIdDoc = JsonDocument.Parse($"\"{batchExecutionId}\"");

        merged["_sys_batch_index"] = indexDoc.RootElement.Clone();
        merged["_sys_batch_total"] = totalDoc.RootElement.Clone();
        merged["_sys_batch_execution_id"] = execIdDoc.RootElement.Clone();

        return JsonSerializer.Serialize(merged);
    }

    private static string BuildArrayJson(List<string> itemJsonObjects)
    {
        var elements = new List<JsonElement>(itemJsonObjects.Count);
        var docs = new List<JsonDocument>(itemJsonObjects.Count);
        try
        {
            foreach (string json in itemJsonObjects)
            {
                JsonDocument doc = JsonDocument.Parse(json);
                docs.Add(doc);
                elements.Add(doc.RootElement);
            }
            return JsonSerializer.Serialize(elements);
        }
        finally
        {
            foreach (JsonDocument doc in docs)
            {
                doc.Dispose();
            }
        }
    }

    private static byte[] BuildZip(IReadOnlyList<(string FileName, byte[] Bytes)> entries)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string fileName, byte[] bytes) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                using Stream entryStream = entry.Open();
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        return memoryStream.ToArray();
    }

    private static string ResolveFileName(
        string pattern,
        string reportName,
        int index,
        Guid batchExecutionId,
        string extension)
    {
        string name = pattern
            .Replace("{reportName}", SanitizeFileName(reportName), StringComparison.OrdinalIgnoreCase)
            .Replace("{index}", index.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{batchExecutionId}", batchExecutionId.ToString("N"), StringComparison.OrdinalIgnoreCase);

        if (!name.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
        {
            name = $"{name}.{extension}";
        }

        return name;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private static ReportOutputFormat FormatForMode(RenderMode mode) => mode switch
    {
        RenderMode.PreviewHtml => ReportOutputFormat.Html,
        _                      => ReportOutputFormat.Pdf,
    };
}
