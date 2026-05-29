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

    /// <summary>
    /// Scale factor applied to the rendered page content (0.1 – 2.0).
    /// Defaults to <c>1.0</c>.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>Top margin in inches. Defaults to <c>0.4</c>.</summary>
    public double MarginTopInches { get; set; } = 0.4;

    /// <summary>Bottom margin in inches. Defaults to <c>0.4</c>.</summary>
    public double MarginBottomInches { get; set; } = 0.4;

    /// <summary>Left margin in inches. Defaults to <c>0.4</c>.</summary>
    public double MarginLeftInches { get; set; } = 0.4;

    /// <summary>Right margin in inches. Defaults to <c>0.4</c>.</summary>
    public double MarginRightInches { get; set; } = 0.4;
}
