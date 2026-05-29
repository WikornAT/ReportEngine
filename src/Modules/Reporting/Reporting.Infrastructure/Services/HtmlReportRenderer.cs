using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

using Scriban;
using Scriban.Runtime;
using Templates.Application.Contracts;
using Templates.Domain.Enums;
using Templates.Domain.ReportTemplates;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Renders a report by:
/// <list type="number">
///   <item>Loading the <see cref="ReportTemplate"/> associated with the report definition.</item>
///   <item>Merging the data JSON into the HTML via <c>{{key}}</c> token substitution.</item>
///   <item>
///     For <see cref="ReportOutputFormat.Pdf"/>: delegating to <see cref="IHtmlToPdfRenderer"/>
///     (Playwright/Chromium).
///   </item>
///   <item>
///     For <see cref="ReportOutputFormat.Html"/>: returning the merged HTML directly.
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
    private readonly HtmlRendererOptions _rendererOptions;
    private readonly ILogger<HtmlReportRenderer> _logger;

    public HtmlReportRenderer(
        IReportTemplateRepository templateRepository,
        IReportingDbContext reportingDbContext,
        IHtmlToPdfRenderer pdfRenderer,
        IOptions<HtmlRendererOptions> rendererOptions,
        ILogger<HtmlReportRenderer> logger)
    {
        _templateRepository = templateRepository;
        _reportingDbContext = reportingDbContext;
        _pdfRenderer = pdfRenderer;
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
        long started = Environment.TickCount64;

        // ── 1. Load report definition to get TemplateId ───────────────────
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

        // ── 2. Load template by its own Id ────────────────────────────────
        ReportTemplate? template = await _templateRepository.GetByIdAsync(
            definition.TemplateId.Value, cancellationToken);

        if (template is null)
        {
            throw new InvalidOperationException(
                $"ReportTemplate '{definition.TemplateId}' assigned to ReportDefinition " +
                $"'{definition.Name}' was not found in the Templates repository.");
        }

        // ── 3. Validate template status ───────────────────────────────────
        if (template.Status != TemplateStatus.Active)
        {
            throw new InvalidOperationException(
                $"Template '{template.Name}' is in '{template.Status}' status and cannot be used for rendering.");
        }

        // ── 4. Merge data into HTML ────────────────────────────────────────
        string mergedHtml = MergeData(template.HtmlContent, template.CssContent, dataJson);

        // ── 5. Render ──────────────────────────────────────────────────────
        RenderedReport result;

        if (outputFormat == ReportOutputFormat.Html)
        {
            byte[] htmlBytes = Encoding.UTF8.GetBytes(mergedHtml);
            result = new RenderedReport(
                FileName: $"report_{reportDefinitionId:N}.html",
                ContentType: "text/html; charset=utf-8",
                Content: htmlBytes);
        }
        else if (outputFormat == ReportOutputFormat.Pdf)
        {
            HtmlPdfRenderOptions pdfOptions = BuildPdfOptions(template);

            byte[] pdfBytes = await _pdfRenderer.RenderPdfAsync(
                mergedHtml, pdfOptions, cancellationToken);

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

        _logRendered(_logger, reportDefinitionId, Environment.TickCount64 - started, null);

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Merges data JSON into the HTML template using Scriban.
    /// Supports scalar tokens <c>{{ field }}</c>, loops <c>{{ for item in items }}...{{ end }}</c>,
    /// conditionals, and all other Scriban template features.
    /// </summary>
    private static string MergeData(string htmlContent, string? cssContent, string dataJson)
    {
        string html = htmlContent;

        // Inject font stylesheet link so @font-face rules are always available
        const string FontCssLink = "<link rel=\"stylesheet\" href=\"/api/designer/fonts/css\">";
        if (!html.Contains("/api/designer/fonts/css", StringComparison.OrdinalIgnoreCase))
        {
            html = html.Replace("</head>", $"{FontCssLink}\n</head>", StringComparison.OrdinalIgnoreCase);
        }

        // Inject supplementary CSS into <head> when present
        if (!string.IsNullOrWhiteSpace(cssContent))
        {
            string styleTag = $"\n<style>\n{cssContent}\n</style>\n";
            html = html.Replace("</head>", $"{styleTag}</head>", StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(dataJson) || dataJson == "{}")
        {
            return html;
        }

        // Build Scriban script object from the JSON payload
        var scriptObject = new ScriptObject();

        using JsonDocument doc = JsonDocument.Parse(dataJson);
        foreach (JsonProperty property in doc.RootElement.EnumerateObject())
        {
            scriptObject.Add(
                property.Name,
                JsonElementToScribanValue(property.Value));
        }

        var context = new TemplateContext { StrictVariables = false };
        context.PushGlobal(scriptObject);

        Template scribanTemplate = Template.Parse(html);
        return scribanTemplate.Render(context);
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to a CLR value that Scriban can work with.
    /// Arrays become <see cref="List{T}"/> of <see cref="ScriptObject"/> (for objects)
    /// or plain values (for primitive arrays).
    /// </summary>
    private static object? JsonElementToScribanValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String  => element.GetString(),
            JsonValueKind.Number  => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True    => true,
            JsonValueKind.False   => false,
            JsonValueKind.Null    => null,
            JsonValueKind.Array   => element.EnumerateArray()
                                        .Select(JsonElementToScribanValue)
                                        .ToList(),
            JsonValueKind.Object  => JsonObjectToScriptObject(element),
            _                     => element.GetRawText()
        };

    private static ScriptObject JsonObjectToScriptObject(JsonElement element)
    {
        var obj = new ScriptObject();
        foreach (JsonProperty prop in element.EnumerateObject())
        {
            obj.Add(prop.Name, JsonElementToScribanValue(prop.Value));
        }
        return obj;
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
