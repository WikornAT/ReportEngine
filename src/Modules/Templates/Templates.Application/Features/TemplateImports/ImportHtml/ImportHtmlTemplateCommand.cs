using MediatR;

using ReportEngine.SharedKernel;

using Templates.Application.DTOs;
using Templates.Domain.Enums;

namespace Templates.Application.Features.TemplateImports.ImportHtml;

/// <summary>
/// Imports a report template from uploaded HTML (and optional CSS/asset) files.
/// <para>
/// The handler will:
/// <list type="number">
///   <item>Validate inputs.</item>
///   <item>Sanitize HTML.</item>
///   <item>Persist any asset files and register <c>TemplateAsset</c> records.</item>
///   <item>Rewrite relative asset references to secure API URLs.</item>
///   <item>Create or update the <c>ReportTemplate</c>.</item>
///   <item>Take a version snapshot.</item>
/// </list>
/// </para>
/// </summary>
public sealed record ImportHtmlTemplateCommand(
    byte[] HtmlFileContent,
    string HtmlFileName,
    byte[]? CssFileContent,
    string? CssFileName,
    IReadOnlyDictionary<string, byte[]> Assets,
    string TemplateCode,
    string Name,
    string? Description,
    PaperSize PaperSize,
    PageOrientation Orientation,
    int WidthPx,
    int HeightPx,
    bool AllowExternalAssets = false
) : IRequest<Result<TemplateImportResultDto>>;
