using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using Reporting.Domain.RenderLogs;
using Reporting.Domain.ReportDefinitions;
using Reporting.Domain.ReportExecutions;
using Reporting.Infrastructure.Persistence.Configurations;

namespace Reporting.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> for the Reporting module.
/// Implements <see cref="IReportingDbContext"/> so the Application layer
/// depends only on the interface, not the concrete type.
/// </summary>
public sealed class ReportingDbContext : DbContext, IReportingDbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportExecution> ReportExecutions => Set<ReportExecution>();
    public DbSet<RenderLog> RenderLogs => Set<RenderLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
