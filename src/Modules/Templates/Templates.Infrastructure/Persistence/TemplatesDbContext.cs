using Microsoft.EntityFrameworkCore;

using Templates.Application.Contracts;
using Templates.Domain.ReportTemplates;
using Templates.Infrastructure.Persistence.Configurations;

namespace Templates.Infrastructure.Persistence;

public sealed class TemplatesDbContext : DbContext, ITemplatesDbContext
{
    public TemplatesDbContext(DbContextOptions<TemplatesDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("templates");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TemplatesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
