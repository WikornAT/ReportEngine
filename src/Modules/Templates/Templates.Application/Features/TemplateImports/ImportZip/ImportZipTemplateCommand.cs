using MediatR;

using ReportEngine.SharedKernel;

using Templates.Application.DTOs;
using Templates.Domain.Enums;

namespace Templates.Application.Features.TemplateImports.ImportZip;

/// <summary>
/// Imports a report template from a ZIP archive.
/// </summary>
/// <remarks>
/// ZIP structure convention:
/// <code>
/// template.html   ← required (must be named exactly template.html)
/// style.css       ← optional
/// assets/         ← optional folder
///   logo.png
///   background.png
/// fonts/          ← optional folder
///   Sarabun-Regular.ttf
/// </code>
/// </remarks>
public sealed record ImportZipTemplateCommand(
    byte[] ZipFileContent,
    string ZipFileName,
    string TemplateCode,
    string Name,
    string? Description,
    PaperSize PaperSize,
    PageOrientation Orientation,
    int WidthPx,
    int HeightPx,
    bool AllowExternalAssets = false
) : IRequest<Result<TemplateImportResultDto>>;
