namespace Designer.Application.DTOs;

/// <summary>
/// Describes a font family with all its available variants.
/// Used by the report designer UI to populate font pickers and
/// by the CSS generator to emit @font-face blocks.
/// </summary>
/// <param name="Family">CSS font-family name, e.g. "Sarabun".</param>
/// <param name="DisplayName">Human-readable label shown in the UI, e.g. "Sarabun".</param>
/// <param name="Variants">All weight/style variants available for this family.</param>
public sealed record FontFaceDto(
    string Family,
    string DisplayName,
    IReadOnlyList<FontVariantDto> Variants);
