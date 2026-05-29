using Templates.Domain.Enums;

namespace Templates.Application.DTOs;

/// <summary>Read-model DTO for a <see cref="Domain.ReportTemplates.ReportTemplate"/>.</summary>
public sealed record ReportTemplateDto(
    Guid Id,
    string Name,
    string? TemplateCode,
    string? Description,
    string HtmlContent,
    string? CssContent,
    PaperSize PaperSize,
    PageOrientation Orientation,
    int WidthPx,
    int HeightPx,
    int Version,
    TemplateStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? ModifiedAt,
    string? ModifiedBy);
