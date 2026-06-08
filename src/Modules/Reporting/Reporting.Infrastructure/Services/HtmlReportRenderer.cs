using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Reporting.Application.Contracts;
using Reporting.Application.Models;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

using Scriban.Runtime;
using Templates.Application.Contracts;
using Templates.Domain.Enums;
using Templates.Domain.ReportTemplates;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Renders a report by:
/// <list type="number">
///   <item>Loading the <see cref="ReportTemplate"/> associated with the report definition.</item>
///   <item>Building a <see cref="TemplateRenderContext"/> from the data JSON.</item>
///   <item>Binding the template via <see cref="ITemplateBindingEngine"/> (Scriban).</item>
///   <item>Injecting font CSS and template CSS into &lt;head&gt;.</item>
///   <item>
///     For <see cref="ReportOutputFormat.Pdf"/>: delegating to <see cref="IHtmlToPdfRenderer"/>.
///   </item>
///   <item>
///     For <see cref="ReportOutputFormat.Html"/>: returning the bound HTML directly.
///   </item>
/// </list>
/// </summary>
internal sealed class HtmlReportRenderer : IReportRenderer
{
    private static readonly Action<ILogger, Guid, string, Exception?> _logRendering =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(10, "HtmlReportRendering"),
            "Rendering report {ReportDefinitionId} as {Format}");

    private static readonly Action<ILogger, Guid, long, Exception?> _logRendered =
        LoggerMessage.Define<Guid, long>(
            LogLevel.Information,
            new EventId(11, "HtmlReportRendered"),
            "Report {ReportDefinitionId} rendered in {ElapsedMs}ms");

    private readonly IReportTemplateRepository _templateRepository;
    private readonly IReportingDbContext _reportingDbContext;
    private readonly IHtmlToPdfRenderer _pdfRenderer;
    private readonly ITemplateBindingEngine _bindingEngine;
    private readonly HtmlRendererOptions _rendererOptions;
    private readonly ILogger<HtmlReportRenderer> _logger;

    public HtmlReportRenderer(
        IReportTemplateRepository templateRepository,
        IReportingDbContext reportingDbContext,
        IHtmlToPdfRenderer pdfRenderer,
        ITemplateBindingEngine bindingEngine,
        IOptions<HtmlRendererOptions> rendererOptions,
        ILogger<HtmlReportRenderer> logger)
    {
        _templateRepository = templateRepository;
        _reportingDbContext = reportingDbContext;
        _pdfRenderer = pdfRenderer;
        _bindingEngine = bindingEngine;
        _rendererOptions = rendererOptions.Value;
        _logger = logger;
    }

    public async Task<RenderedReport> RenderAsync(
        Guid reportDefinitionId,
        string dataJson,
        ReportOutputFormat outputFormat,
        CancellationToken cancellationToken = default)
    {
        _logRendering(_logger, reportDefinitionId, outputFormat.ToString(), null);
        long totalStart = Environment.TickCount64;

        // ── 1. Load report definition ─────────────────────────────────────
        ReportDefinition? definition = await _reportingDbContext.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == reportDefinitionId, cancellationToken);

        if (definition is null)
        {
            throw new InvalidOperationException(
                $"ReportDefinition '{reportDefinitionId}' not found.");
        }

        if (definition.TemplateId is null)
        {
            throw new InvalidOperationException(
                $"ReportDefinition '{definition.Name}' has no template assigned. " +
                "Call AssignTemplate before executing.");
        }

        // ── 2. Load template ──────────────────────────────────────────────
        ReportTemplate? template = await _templateRepository.GetByIdAsync(
            definition.TemplateId.Value, cancellationToken);

        if (template is null)
        {
            throw new InvalidOperationException(
                $"ReportTemplate '{definition.TemplateId}' assigned to ReportDefinition " +
                $"'{definition.Name}' was not found in the Templates repository.");
        }

        if (template.Status != TemplateStatus.Active)
        {
            throw new InvalidOperationException(
                $"Template '{template.Name}' is in '{template.Status}' status and cannot be used for rendering.");
        }

        // ── 3. Build TemplateRenderContext from dataJson ───────────────────
        // dataJson may be a JSON object (single page) or a JSON array (multi-page).
        string htmlWithCss = InjectCss(template.HtmlContent, template.CssContent);

        long bindStart = Environment.TickCount64;
        string boundHtml = await BindHtmlAsync(dataJson, definition, htmlWithCss, cancellationToken);
        long templateBindingMs = Environment.TickCount64 - bindStart;

        // ── 5. Render ─────────────────────────────────────────────────────
        long renderStart = Environment.TickCount64;
        RenderedReport result;

        if (outputFormat == ReportOutputFormat.Html)
        {
            byte[] htmlBytes = Encoding.UTF8.GetBytes(boundHtml);
            result = new RenderedReport(
                FileName: $"report_{reportDefinitionId:N}.html",
                ContentType: "text/html; charset=utf-8",
                Content: htmlBytes);
        }
        else if (outputFormat == ReportOutputFormat.Pdf)
        {
            HtmlPdfRenderOptions pdfOptions = BuildPdfOptions(template);

            byte[] pdfBytes = await _pdfRenderer.RenderPdfAsync(
                boundHtml, pdfOptions, cancellationToken);

            result = new RenderedReport(
                FileName: $"report_{reportDefinitionId:N}.pdf",
                ContentType: "application/pdf",
                Content: pdfBytes);
        }
        else
        {
            throw new NotSupportedException(
                $"HtmlReportRenderer does not support output format '{outputFormat}'. " +
                "Use Html or Pdf.");
        }

        long renderMs = Environment.TickCount64 - renderStart;

        _logRendered(_logger, reportDefinitionId, Environment.TickCount64 - totalStart, null);

        return result with { PhaseTimings = new RenderPhaseTimings(0, templateBindingMs, renderMs) };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Binds <paramref name="htmlWithCss"/> against <paramref name="dataJson"/>.
    /// <para>
    /// <b>Object</b>: single-page — returns one bound HTML string.<br/>
    /// <b>Array</b>: multi-page — binds each element separately and concatenates the
    /// resulting <c>&lt;body&gt;</c> contents inside a shared <c>&lt;html&gt;</c> wrapper,
    /// inserting a <c>page-break-after: always</c> div between pages so Playwright
    /// produces one PDF page per array element.
    /// </para>
    /// </summary>
    private async Task<string> BindHtmlAsync(
        string dataJson,
        ReportDefinition definition,
        string htmlWithCss,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
        {
            return await BindSinglePageAsync(dataJson, definition, htmlWithCss, cancellationToken);
        }

        using JsonDocument doc = JsonDocument.Parse(dataJson);

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return await BindSinglePageAsync(dataJson, definition, htmlWithCss, cancellationToken);
        }

        // Multi-page: bind each element, then compose a single HTML document
        var pageHtmlList = new List<string>();
        foreach (JsonElement element in doc.RootElement.EnumerateArray())
        {
            string elementJson = element.GetRawText();
            string pageHtml = await BindSinglePageAsync(elementJson, definition, htmlWithCss, cancellationToken);
            pageHtmlList.Add(ExtractBodyContent(pageHtml));
        }

        return ComposeMultiPageHtml(htmlWithCss, pageHtmlList);
    }

    private async Task<string> BindSinglePageAsync(
        string dataJson,
        ReportDefinition definition,
        string htmlWithCss,
        CancellationToken cancellationToken)
    {
        ParseDataJson(dataJson, out Dictionary<string, object?> paramsDict, out Dictionary<string, object?> dataDict);
        TemplateRenderContext renderContext = BuildRenderContext(definition, paramsDict, dataDict);
        return await _bindingEngine.BindAsync(htmlWithCss, renderContext, cancellationToken);
    }

    /// <summary>Extracts the inner HTML of the &lt;body&gt; tag, or the full string if none found.</summary>
    private static string ExtractBodyContent(string html)
    {
        int bodyStart = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        int bodyClose = bodyStart >= 0 ? html.IndexOf('>', bodyStart) : -1;
        int bodyEnd   = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        if (bodyStart < 0 || bodyClose < 0 || bodyEnd < 0)
        {
            return html;
        }

        return html[(bodyClose + 1)..bodyEnd];
    }

    /// <summary>
    /// Wraps all page body fragments inside the outer HTML shell (head + styles) from
    /// <paramref name="htmlTemplate"/>, separated by a page-break div.
    /// </summary>
    private static string ComposeMultiPageHtml(string htmlTemplate, IReadOnlyList<string> pageBodyFragments)
    {
        const string pageBreak = "<div style=\"page-break-after:always;\"></div>";

        int bodyStart = htmlTemplate.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        int bodyClose = bodyStart >= 0 ? htmlTemplate.IndexOf('>', bodyStart) : -1;
        int bodyEnd   = htmlTemplate.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        string openTag  = bodyStart >= 0 && bodyClose >= 0
            ? htmlTemplate[bodyStart..(bodyClose + 1)]
            : "<body>";
        string closeTag = "</body></html>";
        string head     = bodyStart >= 0
            ? htmlTemplate[..bodyStart]
            : "<!DOCTYPE html><html>";

        _ = bodyEnd; // used for reference only; we rebuild the body from fragments

        string combinedBody = string.Join(pageBreak, pageBodyFragments);
        return $"{head}{openTag}{combinedBody}{closeTag}";
    }

    /// <summary>
    /// Parses a flat <paramref name="dataJson"/> string into two dictionaries.
    /// Keys prefixed with <c>_data_</c> go to <paramref name="dataDict"/> (prefix stripped);
    /// all other keys go to <paramref name="paramsDict"/>.
    /// </summary>
    private static void ParseDataJson(
        string dataJson,
        out Dictionary<string, object?> paramsDict,
        out Dictionary<string, object?> dataDict)
    {
        paramsDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        dataDict   = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(dataJson) || dataJson.Trim() == "{}")
        {
            return;
        }

        using JsonDocument doc = JsonDocument.Parse(dataJson);

        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            object? value = JsonElementToValue(prop.Value);

            if (prop.Name.StartsWith("_data_", StringComparison.OrdinalIgnoreCase))
            {
                dataDict[prop.Name[6..]] = value;
            }
            else
            {
                paramsDict[prop.Name] = value;
            }
        }
    }

    /// <summary>
    /// Builds a <see cref="TemplateRenderContext"/> from pre-resolved parameter and data
    /// dictionaries. The dicts are produced by <see cref="ParseDataJson"/> (or supplied
    /// directly by callers that have already executed data sources).
    /// </summary>
    private static TemplateRenderContext BuildRenderContext(
        ReportDefinition definition,
        IReadOnlyDictionary<string, object?> paramsDict,
        IReadOnlyDictionary<string, object?> dataDict)
    {
        var system = new SystemContext(
            Now: DateTimeOffset.UtcNow,
            Today: DateOnly.FromDateTime(DateTime.UtcNow),
            User: "system");

        return new TemplateRenderContext(
            Report: ReportMeta.From(definition),
            Params: paramsDict,
            Data: dataDict,
            System: system);
    }

    private static string InjectCss(string htmlContent, string? cssContent)
    {
        string html = htmlContent;

        // Inject font stylesheet so @font-face rules are always available
        const string FontCssLink = "<link rel=\"stylesheet\" href=\"/api/designer/fonts/css\">";
        if (!html.Contains("/api/designer/fonts/css", StringComparison.OrdinalIgnoreCase))
        {
            html = html.Replace("</head>", $"{FontCssLink}\n</head>", StringComparison.OrdinalIgnoreCase);
        }

        // Inject template-specific CSS
        if (!string.IsNullOrWhiteSpace(cssContent))
        {
            string styleTag = $"\n<style>\n{cssContent}\n</style>\n";
            html = html.Replace("</head>", $"{styleTag}</head>", StringComparison.OrdinalIgnoreCase);
        }

        return html;
    }

    private static object? JsonElementToValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String  => element.GetString(),
            JsonValueKind.Number  => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True    => true,
            JsonValueKind.False   => false,
            JsonValueKind.Null    => null,
            JsonValueKind.Array   => element.EnumerateArray().Select(JsonElementToValue).ToList(),
            JsonValueKind.Object  => JsonObjectToDict(element),
            _                     => element.GetRawText()
        };

    private static Dictionary<string, object?> JsonObjectToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty prop in element.EnumerateObject())
        {
            dict[prop.Name] = JsonElementToValue(prop.Value);
        }
        return dict;
    }

    private HtmlPdfRenderOptions BuildPdfOptions(ReportTemplate template)
    {
        (double widthIn, double heightIn) = template.PaperSize switch
        {
            PaperSize.A4     => (8.27, 11.69),
            PaperSize.A3     => (11.69, 16.54),
            PaperSize.Letter => (8.5, 11.0),
            PaperSize.Legal  => (8.5, 14.0),
            _                => (8.27, 11.69)
        };

        bool landscape = template.Orientation == PageOrientation.Landscape;

        return new HtmlPdfRenderOptions(
            PaperWidth: landscape ? heightIn : widthIn,
            PaperHeight: landscape ? widthIn : heightIn,
            Landscape: landscape,
            PrintBackground: true,
            MarginTopInches: _rendererOptions.MarginTopInches,
            MarginBottomInches: _rendererOptions.MarginBottomInches,
            MarginLeftInches: _rendererOptions.MarginLeftInches,
            MarginRightInches: _rendererOptions.MarginRightInches,
            Scale: _rendererOptions.Scale,
            BaseUrl: _rendererOptions.AssetBaseUrl);
    }
}
