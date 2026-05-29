using System.Text;

using Designer.Application.Contracts;
using Designer.Application.DTOs;

using Microsoft.AspNetCore.Hosting;

namespace Designer.Infrastructure.Services;

/// <summary>
/// Scans <c>wwwroot/fonts</c> and builds a normalised font catalog.
/// <para>
/// <b>Safety:</b> Only files whose resolved absolute path starts with the
/// canonical <c>wwwroot/fonts</c> directory are included — no symlink or
/// path-traversal escapes are exposed. Physical server paths are never
/// returned to callers.
/// </para>
/// <para>
/// <b>Supported extensions:</b> <c>.ttf</c>, <c>.woff2</c>.
/// </para>
/// <para>
/// <b>File-name convention:</b> <c>{Family}-{Variant}.{ext}</c><br/>
/// Examples: <c>Sarabun-Regular.ttf</c>, <c>Sarabun-BoldItalic.ttf</c>,
/// <c>Sarabun-ExtraLight.ttf</c>.
/// </para>
/// </summary>
internal sealed class FontCatalogService : IFontCatalogService
{
    private readonly string _fontsRoot;

    // Lazily built catalog — scanned once and cached for the process lifetime.
    private IReadOnlyList<FontFaceDto>? _cache;
    private readonly Lock _lock = new();

    public FontCatalogService(IWebHostEnvironment env)
    {
        _fontsRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "fonts"));
    }

    // ── IFontCatalogService ───────────────────────────────────────────────────

    public IReadOnlyList<FontFaceDto> GetFonts()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        lock (_lock)
        {
            if (_cache is not null)
            {
                return _cache;
            }

            _cache = BuildCatalog();
        }

        return _cache;
    }

    public string GetFontFaceCss()
    {
        IReadOnlyList<FontFaceDto> fonts = GetFonts();

        var sb = new StringBuilder();
        sb.AppendLine("/* Auto-generated @font-face rules — do not edit manually */");

        foreach (FontFaceDto family in fonts)
        {
            foreach (FontVariantDto v in family.Variants)
            {
                sb.AppendLine("@font-face {");
                sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"  font-family: '{family.Family}';"));
                sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"  src: url('{v.Url}') format('{v.Format}');"));
                sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"  font-weight: {v.Weight};"));
                sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"  font-style: {v.Style};"));
                sb.AppendLine("  font-display: swap;");
                sb.AppendLine("}");
            }
        }

        return sb.ToString();
    }

    // ── Scanning ──────────────────────────────────────────────────────────────

    private List<FontFaceDto> BuildCatalog()
    {
        if (!Directory.Exists(_fontsRoot))
        {
            return [];
        }

        var variants = new Dictionary<string, List<FontVariantDto>>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.EnumerateFiles(_fontsRoot)
            .Where(IsAllowedFontFile)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            // Safety: ensure resolved path stays inside _fontsRoot
            string resolved = Path.GetFullPath(file);
            if (!resolved.StartsWith(_fontsRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fileName = Path.GetFileName(file);
            string nameNoExt = Path.GetFileNameWithoutExtension(file);
            string ext = Path.GetExtension(file).ToLowerInvariant();

            (string family, int weight, string style) = ParseFontFileName(nameNoExt);
            string url = $"/fonts/{fileName}";
            string format = ExtToFormat(ext);

            if (!variants.TryGetValue(family, out List<FontVariantDto>? list))
            {
                list = [];
                variants[family] = list;
            }

            list.Add(new FontVariantDto(
                Weight: weight,
                Style: style,
                FileName: fileName,
                Url: url,
                Format: format));
        }

        return variants
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new FontFaceDto(
                Family: kv.Key,
                DisplayName: kv.Key,
                Variants: kv.Value
                    .OrderBy(v => v.Weight)
                    .ThenBy(v => v.Style)
                    .ToList()))
            .ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsAllowedFontFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".ttf" or ".woff2";
    }

    private static string ExtToFormat(string ext) =>
        ext switch
        {
            ".ttf"   => "truetype",
            ".woff2" => "woff2",
            _        => "truetype"
        };

    /// <summary>
    /// Parses "Sarabun-BoldItalic" → (family:"Sarabun", weight:700, style:"italic").
    /// Falls back to weight 400 / style "normal" for unknown variants.
    /// </summary>
    internal static (string Family, int Weight, string Style) ParseFontFileName(string nameNoExt)
    {
        int dash = nameNoExt.IndexOf('-');
        if (dash < 0)
        {
            return (nameNoExt, 400, "normal");
        }

        string family  = nameNoExt[..dash];
        string variant = nameNoExt[(dash + 1)..];

        bool italic = variant.EndsWith("Italic", StringComparison.OrdinalIgnoreCase);
        string weightPart = italic
            ? variant[..^"Italic".Length]
            : variant;

        int weight = weightPart.ToLowerInvariant() switch
        {
            "thin"       => 100,
            "extralight" => 200,
            "light"      => 300,
            "regular"    => 400,
            ""           => 400,
            "medium"     => 500,
            "semibold"   => 600,
            "bold"       => 700,
            "extrabold"  => 800,
            "black"      => 900,
            _            => 400
        };

        return (family, weight, italic ? "italic" : "normal");
    }
}
