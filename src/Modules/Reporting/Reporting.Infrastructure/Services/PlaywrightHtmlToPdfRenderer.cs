using Microsoft.Playwright;

using Reporting.Application.Contracts;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Converts an HTML document to PDF using Playwright (Chromium headless).
/// <para>
/// <b>Asset resolution:</b> When <see cref="HtmlPdfRenderOptions.BaseUrl"/> is set,
/// Playwright navigates to <c>about:blank</c> and then sets the base URL via
/// <c>goto</c> with <c>waitUntil: networkidle</c>, allowing relative paths such as
/// <c>/fonts/THSarabun.ttf</c> and <c>/uploads/documents/logo.png</c> to resolve
/// against the host application.
/// </para>
/// <para>
/// <b>PrintBackground</b> is always respected from <see cref="HtmlPdfRenderOptions.PrintBackground"/>.
/// Set it to <see langword="true"/> for styled HTML templates with background images and colours.
/// </para>
/// <para>
/// <b>Thread-safety:</b> A single <see cref="IPlaywright"/> and <see cref="IBrowser"/> instance
/// is created on first use and reused for the lifetime of this service (registered as Singleton).
/// The browser is disposed when the service is disposed.
/// </para>
/// </summary>
internal sealed class PlaywrightHtmlToPdfRenderer : IHtmlToPdfRenderer, IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // ── IHtmlToPdfRenderer ────────────────────────────────────────────────────

    public async Task<byte[]> RenderPdfAsync(
        string htmlContent,
        HtmlPdfRenderOptions options,
        CancellationToken cancellationToken = default)
    {
        IBrowser browser = await GetBrowserAsync(cancellationToken);

        await using IPage page = await browser.NewPageAsync();

        // Set viewport to match the template's design canvas
        await page.SetViewportSizeAsync(
            width: (int)(options.PaperWidth * 96),   // inches → px at 96dpi
            height: (int)(options.PaperHeight * 96));

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            // Navigate to the base URL first so relative asset paths resolve correctly,
            // then replace the page content with our HTML.
            await page.GotoAsync(options.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 10_000
            });

            await page.SetContentAsync(htmlContent, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30_000
            });
        }
        else
        {
            await page.SetContentAsync(htmlContent, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30_000
            });
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Format = MapPaperFormat(options.PaperWidth, options.PaperHeight, options.Landscape),
            Landscape = options.Landscape,
            PrintBackground = options.PrintBackground,
            Scale = (float)options.Scale,
            Margin = new Margin
            {
                Top = $"{options.MarginTopInches}in",
                Bottom = $"{options.MarginBottomInches}in",
                Left = $"{options.MarginLeftInches}in",
                Right = $"{options.MarginRightInches}in",
            }
        });

        return pdfBytes;
    }

    // ── Browser lifecycle ─────────────────────────────────────────────────────

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            return _browser;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is not null)
            {
                return _browser;
            }

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args =
                [
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",   // required for Docker/Linux
                    "--disable-gpu",
                    "--font-render-hinting=none" // consistent Thai font rendering
                ]
            });
        }
        finally
        {
            _initLock.Release();
        }

        return _browser;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps pixel dimensions to a named Playwright paper format when one of the
    /// well-known sizes matches; otherwise returns <see langword="null"/> (falling back to
    /// explicit Width/Height).
    /// </summary>
    private static string? MapPaperFormat(double widthIn, double heightIn, bool landscape)
    {
        // A4: 8.27 × 11.69 in
        bool isA4 = IsApprox(widthIn, landscape ? 11.69 : 8.27) &&
                    IsApprox(heightIn, landscape ? 8.27 : 11.69);
        if (isA4) { return "A4"; }

        // Letter: 8.5 × 11 in
        bool isLetter = IsApprox(widthIn, landscape ? 11.0 : 8.5) &&
                        IsApprox(heightIn, landscape ? 8.5 : 11.0);
        if (isLetter) { return "Letter"; }

        return null;

        static bool IsApprox(double a, double b) => Math.Abs(a - b) < 0.05;
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
