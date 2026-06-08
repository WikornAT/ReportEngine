using Microsoft.EntityFrameworkCore;

using Templates.Domain.ReportTemplates;

namespace Templates.Application.Contracts;

/// <summary>
/// Unit-of-work abstraction over the Templates module's EF Core DbContext.
/// </summary>
public interface ITemplatesDbContext
{
    DbSet<ReportTemplate> ReportTemplates { get; }
    DbSet<TemplateAsset> TemplateAssets { get; }
    DbSet<TemplateVersion> TemplateVersions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
