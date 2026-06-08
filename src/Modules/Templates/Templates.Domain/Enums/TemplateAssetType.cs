namespace Templates.Domain.Enums;

/// <summary>Classifies the type of a <see cref="ReportTemplates.TemplateAsset"/>.</summary>
public enum TemplateAssetType
{
    /// <summary>A raster or vector image (PNG, JPG, JPEG, SVG, WebP).</summary>
    Image = 0,

    /// <summary>A font file (TTF, OTF, WOFF, WOFF2).</summary>
    Font = 1,

    /// <summary>A supplementary CSS file.</summary>
    Stylesheet = 2,

    /// <summary>Any other allowed asset type.</summary>
    Other = 99,
}
