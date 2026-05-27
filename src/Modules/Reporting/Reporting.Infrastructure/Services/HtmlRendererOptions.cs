namespace Reporting.Infrastructure.Services;

/// <summary>
/// Configuration options for <see cref="HtmlReportRenderer"/>.
/// Bind from <c>appsettings.json</c> under the key <c>"Reporting:HtmlRenderer"</c>.
/// </summary>
public sealed class HtmlRendererOptions
{
    public const string SectionName = "Reporting:HtmlRenderer";

    /// <summary>
    /// Base URL passed to Playwright so that relative asset paths
    /// (<c>/fonts/…</c>, <c>/uploads/…</c>) resolve correctly.
    /// Example: <c>http://localhost:5000</c>
    /// </summary>
    public string? AssetBaseUrl { get; set; }
}
