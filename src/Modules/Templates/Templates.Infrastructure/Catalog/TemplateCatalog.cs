using Microsoft.EntityFrameworkCore;

using ReportEngine.Contracts.Templates;

using Templates.Infrastructure.Persistence;

namespace Templates.Infrastructure.Catalog;

/// <summary>
/// Implements <see cref="ITemplateCatalog"/> for the Templates module.
/// Provides a read-only view of template data for cross-module consumers (e.g., Reporting).
/// </summary>
internal sealed class TemplateCatalog : ITemplateCatalog
{
    private readonly TemplatesDbContext _dbContext;

    public TemplateCatalog(TemplatesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TemplateDescriptor?> GetTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.ReportTemplates
            .AsNoTracking()
            .Include(t => t.Assets)
            .Where(t => t.Id == templateId)
            .Select(t => new
            {
                t.Id,
                t.TemplateCode,
                t.Name,
                t.HtmlContent,
                t.CssContent,
                PaperSize = t.PaperSize.ToString(),
                Orientation = t.Orientation.ToString(),
                t.WidthPx,
                t.HeightPx,
                Assets = t.Assets.Select(a => new
                {
                    a.Id,
                    a.RelativePath,
                    a.PublicUrl,
                    a.ContentType,
                    a.FileName,
                }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new TemplateDescriptor(
            Id: row.Id,
            TemplateCode: row.TemplateCode ?? string.Empty,
            Name: row.Name,
            HtmlContent: row.HtmlContent,
            CssContent: row.CssContent,
            PaperSize: row.PaperSize,
            Orientation: row.Orientation,
            WidthPx: row.WidthPx,
            HeightPx: row.HeightPx,
            Assets: row.Assets
                .Select(a => new TemplateAssetDescriptor(
                    Id: a.Id,
                    RelativePath: a.RelativePath,
                    PublicUrl: a.PublicUrl,
                    ContentType: a.ContentType,
                    FileName: a.FileName))
                .ToList());
    }
}
