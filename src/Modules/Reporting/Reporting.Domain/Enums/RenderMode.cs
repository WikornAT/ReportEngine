namespace Reporting.Domain.Enums;

/// <summary>
/// Specifies how a batch render request should produce its output.
/// </summary>
//[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Single is the correct domain term for a one-item render.")]
public enum RenderMode
{
    /// <summary>
    /// Renders a single parameter object as one PDF document.
    /// Exactly one parameter object must be supplied.
    /// </summary>
    SingleFile = 0,

    /// <summary>
    /// Renders multiple parameter objects as individual logical documents and
    /// merges them into one PDF file with page breaks between documents.
    /// </summary>
    MergePdf = 1,

    /// <summary>
    /// Renders each parameter object as a separate PDF file and packages
    /// all files into a single ZIP archive.
    /// </summary>
    ZipPdf = 2,

    /// <summary>
    /// Returns the composed HTML for all parameter objects with page breaks
    /// between documents. Intended for designer preview.
    /// </summary>
    PreviewHtml = 3,
}
