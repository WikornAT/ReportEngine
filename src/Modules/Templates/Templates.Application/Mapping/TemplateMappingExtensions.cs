using Templates.Domain.ReportTemplates;
using Templates.Application.DTOs;

namespace Templates.Application.Mapping;

internal static class TemplateMappingExtensions
{
    public static ReportTemplateDto ToDto(this ReportTemplate t) =>
        new(
            Id: t.Id,
            Name: t.Name,
            TemplateCode: t.TemplateCode,
            Description: t.Description,
            HtmlContent: t.HtmlContent,
            CssContent: t.CssContent,
            PaperSize: t.PaperSize,
            Orientation: t.Orientation,
            WidthPx: t.WidthPx,
            HeightPx: t.HeightPx,
            Version: t.Version,
            Status: t.Status,
            CreatedAt: t.CreatedAt,
            CreatedBy: t.CreatedBy,
            ModifiedAt: t.ModifiedAt,
            ModifiedBy: t.ModifiedBy);
}
