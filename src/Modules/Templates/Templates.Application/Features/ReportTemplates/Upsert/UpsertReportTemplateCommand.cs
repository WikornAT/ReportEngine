using MediatR;

using ReportEngine.SharedKernel;
using Templates.Application.DTOs;
using Templates.Domain.Enums;

namespace Templates.Application.Features.ReportTemplates.Upsert;

/// <summary>
/// Creates a new template or replaces the content of an existing one.
/// When <paramref name="Id"/> is <see langword="null"/> a new template is created.
/// When provided, the existing template's content is updated.
/// </summary>
public sealed record UpsertReportTemplateCommand(
    Guid? Id,
    string Name,
    string HtmlContent,
    string? CssContent,
    string? Description,
    string? TemplateCode,
    PaperSize PaperSize,
    PageOrientation Orientation,
    int WidthPx,
    int HeightPx) : IRequest<Result<ReportTemplateDto>>;
