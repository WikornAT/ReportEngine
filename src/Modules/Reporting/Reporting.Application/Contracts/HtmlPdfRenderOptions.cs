namespace Reporting.Application.Contracts;

/// <summary>
/// Options that control how an HTML document is rendered to PDF by
/// <see cref="IHtmlToPdfRenderer"/>.
/// </summary>
/// <param name="PaperWidth">Page width in inches (e.g., 8.27 for A4).</param>
/// <param name="PaperHeight">Page height in inches (e.g., 11.69 for A4).</param>
/// <param name="Landscape">When <see langword="true"/>, rotates the page to landscape orientation.</param>
/// <param name="PrintBackground">
/// When <see langword="true"/>, includes CSS backgrounds, background images, and colours in the PDF output.
/// Should always be <see langword="true"/> for styled HTML templates.
/// </param>
/// <param name="MarginTopInches">Top margin in inches (default 0 for pixel-perfect layouts).</param>
/// <param name="MarginBottomInches">Bottom margin in inches.</param>
/// <param name="MarginLeftInches">Left margin in inches.</param>
/// <param name="MarginRightInches">Right margin in inches.</param>
/// <param name="Scale">CSS zoom scale factor (0.1–2.0, default 1.0).</param>
/// <param name="BaseUrl">
/// Base URL used by Playwright to resolve relative asset references
/// (fonts at <c>/fonts/…</c>, images at <c>/uploads/…</c>).
/// Example: <c>http://localhost:5000</c> or a file:// URL pointing to an asset root directory.
/// </param>
public sealed record HtmlPdfRenderOptions(
    double PaperWidth = 8.27,
    double PaperHeight = 11.69,
    bool Landscape = false,
    bool PrintBackground = true,
    double MarginTopInches = 0,
    double MarginBottomInches = 0,
    double MarginLeftInches = 0,
    double MarginRightInches = 0,
    double Scale = 1.0,
    string? BaseUrl = null)
{
    /// <summary>Standard A4 portrait options with print-background enabled.</summary>
    public static readonly HtmlPdfRenderOptions A4Portrait = new(
        PaperWidth: 8.27, PaperHeight: 11.69,
        Landscape: false, PrintBackground: true);

    /// <summary>Standard A4 landscape options with print-background enabled.</summary>
    public static readonly HtmlPdfRenderOptions A4Landscape = new(
        PaperWidth: 11.69, PaperHeight: 8.27,
        Landscape: true, PrintBackground: true);
}
