namespace Reporting.Domain.Enums;

/// <summary>
/// Specifies the file format used when rendering a report output file.
/// </summary>
public enum ReportOutputFormat
{
    /// <summary>Adobe Portable Document Format — the default enterprise-grade output.</summary>
    Pdf = 0,

    /// <summary>Microsoft Excel 2007+ Open XML Workbook (.xlsx).</summary>
    Excel = 1,

    /// <summary>Microsoft Word 2007+ Open XML Document (.docx).</summary>
    Word = 2,

    /// <summary>Comma-separated values — plain-text tabular data.</summary>
    Csv = 3,

    /// <summary>JSON-serialized report data payload.</summary>
    Json = 4,

    /// <summary>XML-serialized report data payload.</summary>
    Xml = 5,

    /// <summary>HTML fragment or full document for browser/email rendering.</summary>
    Html = 6,

    /// <summary>Plain text output.</summary>
    Text = 7,

    /// <summary>TIFF image — used for high-quality print or archival scenarios.</summary>
    Tiff = 8,
}
