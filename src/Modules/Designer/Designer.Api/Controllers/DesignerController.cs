using Designer.Application.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace Designer.Api.Controllers;

[ApiController]
[Route("api/designer")]
public sealed class DesignerController : ControllerBase
{
    private readonly IFontCatalogService _fontCatalog;

    public DesignerController(IFontCatalogService fontCatalog)
    {
        _fontCatalog = fontCatalog;
    }

    [HttpGet("ping")]
    public IActionResult Ping() =>
        Ok(new { Module = "Designer", Status = "ok", Timestamp = DateTimeOffset.UtcNow });

    // ── Fonts ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all font families installed under wwwroot/fonts, grouped by family
    /// with normalized variant metadata (weight, style, fileName, url, format).
    /// Used by the report designer UI to populate font pickers.
    /// </summary>
    [HttpGet("fonts")]
    [Produces("application/json")]
    public IActionResult GetFonts() =>
        Ok(_fontCatalog.GetFonts());

    /// <summary>
    /// Returns a ready-to-use CSS block of @font-face rules for every installed
    /// font variant. Embed this inside a &lt;style&gt; tag in your HTML template
    /// so that both browser preview and Playwright PDF rendering use the same fonts.
    /// <para>
    /// Example usage in an HTML template:
    /// <code>
    ///   &lt;link rel="stylesheet" href="/api/designer/fonts/css" /&gt;
    /// </code>
    /// </para>
    /// </summary>
    [HttpGet("fonts/css")]
    [Produces("text/css")]
    public ContentResult GetFontsCss() =>
        Content(_fontCatalog.GetFontFaceCss(), "text/css; charset=utf-8");
}

