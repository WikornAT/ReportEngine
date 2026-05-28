namespace Templates.Domain.Enums;

/// <summary>Paper size used when generating PDF output from an HTML template.</summary>
public enum PaperSize
{
    /// <summary>ISO A4 — 210 × 297 mm (default).</summary>
    A4 = 0,

    /// <summary>ISO A3 — 297 × 420 mm.</summary>
    A3 = 1,

    /// <summary>US Letter — 8.5 × 11 in.</summary>
    Letter = 2,

    /// <summary>US Legal — 8.5 × 14 in.</summary>
    Legal = 3,
}
