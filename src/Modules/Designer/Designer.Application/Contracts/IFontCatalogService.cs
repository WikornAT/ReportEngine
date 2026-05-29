using Designer.Application.DTOs;

namespace Designer.Application.Contracts;

/// <summary>
/// Provides font metadata scanned from the server's font directory.
/// <para>
/// Implemented in <c>Designer.Infrastructure</c> by <c>FontCatalogService</c>,
/// which reads physical <c>.ttf</c> / <c>.woff2</c> files from
/// <c>wwwroot/fonts</c> and derives CSS metadata from each file name.
/// </para>
/// </summary>
public interface IFontCatalogService
{
    /// <summary>
    /// Returns all font families installed on the server, each with their
    /// available weight/style variants and public asset URLs.
    /// </summary>
    IReadOnlyList<FontFaceDto> GetFonts();

    /// <summary>
    /// Generates a complete CSS block of <c>@font-face</c> rules for every
    /// installed font variant.  The block is ready to be embedded in a
    /// <c>&lt;style&gt;</c> tag or served as a standalone <c>.css</c> file.
    /// </summary>
    string GetFontFaceCss();
}
