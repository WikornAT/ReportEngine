namespace Designer.Application.DTOs;

/// <summary>
/// Describes a single font file variant (one @font-face entry).
/// </summary>
/// <param name="Weight">CSS font-weight value, e.g. 400, 700.</param>
/// <param name="Style">CSS font-style value: "normal" or "italic".</param>
/// <param name="FileName">Bare file name, e.g. "Sarabun-Bold.ttf".</param>
/// <param name="Url">Public static-file URL, e.g. "/fonts/Sarabun-Bold.ttf".</param>
/// <param name="Format">CSS @font-face format hint, e.g. "truetype".</param>
public sealed record FontVariantDto(
    int Weight,
    string Style,
    string FileName,
    string Url,
    string Format);
