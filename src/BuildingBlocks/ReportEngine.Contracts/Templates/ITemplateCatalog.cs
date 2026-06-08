namespace ReportEngine.Contracts.Templates;

/// <summary>
/// Cross-module contract allowing the Reporting module to read template
/// content without depending on <c>Templates.Infrastructure</c> or
/// <c>Templates.Application</c> internals.
/// <para>
/// Assets are addressed via <c>/api/templates/report-templates/{templateId}/assets/{assetId}/content</c>.
/// Consumed by <c>Reporting.Infrastructure</c> (e.g., <c>HtmlReportRenderer</c>).
/// </para>
/// </summary>
public interface ITemplateCatalog
{
    /// <summary>
    /// Returns a lightweight descriptor for the given template, or
    /// <see langword="null"/> if no template with that id exists.
    /// </summary>
    Task<TemplateDescriptor?> GetTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight read model returned by <see cref="ITemplateCatalog"/>.
/// Contains everything the Reporting module needs to render a report template.
/// </summary>
public sealed record TemplateDescriptor(
    Guid Id,
    string TemplateCode,
    string Name,
    string HtmlContent,
    string? CssContent,
    string PaperSize,
    string Orientation,
    int WidthPx,
    int HeightPx,
    IReadOnlyList<TemplateAssetDescriptor> Assets);

/// <summary>
/// Lightweight asset descriptor within a <see cref="TemplateDescriptor"/>.
/// </summary>
public sealed record TemplateAssetDescriptor(
    Guid Id,
    string RelativePath,
    string PublicUrl,
    string ContentType,
    string FileName);
