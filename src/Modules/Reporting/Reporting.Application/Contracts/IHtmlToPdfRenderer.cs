namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for converting an HTML document string into a PDF binary.
/// <para>
/// The implementation lives in <c>Reporting.Infrastructure</c> and uses
/// Playwright/Chromium for headless rendering.
/// </para>
/// <para>
/// <b>Asset resolution:</b> Set <see cref="HtmlPdfRenderOptions.BaseUrl"/> to the application's
/// base URL so that relative paths such as <c>/fonts/THSarabun.ttf</c> and
/// <c>/uploads/documents/logo.png</c> resolve correctly inside the headless browser.
/// </para>
/// </summary>
public interface IHtmlToPdfRenderer
{
    /// <summary>
    /// Renders the supplied HTML string to a PDF byte array.
    /// </summary>
    /// <param name="htmlContent">
    /// Full HTML document (including &lt;!DOCTYPE html&gt; and &lt;head&gt; with @font-face rules).
    /// </param>
    /// <param name="options">
    /// Paper size, margins, background-print flag, base URL, and scale settings.
    /// Use <see cref="HtmlPdfRenderOptions.A4Portrait"/> as a sensible default.
    /// </param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>Raw PDF bytes suitable for writing to a file or HTTP response.</returns>
    Task<byte[]> RenderPdfAsync(
        string htmlContent,
        HtmlPdfRenderOptions options,
        CancellationToken cancellationToken = default);
}
