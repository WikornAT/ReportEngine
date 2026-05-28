using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using Reporting.Domain.ReportDefinitions;
using Reporting.Domain.ReportExecutions;

namespace Reporting.Application.UnitTests;

/// <summary>
/// Lightweight EF Core InMemory DbContext used exclusively by application-layer unit tests.
/// Mirrors the entity relationships of ReportingDbContext without production-only configuration
/// (schemas, column types) that are incompatible with the InMemory provider.
/// </summary>
internal sealed class InMemoryReportingDbContext : DbContext, IReportingDbContext
{
    public InMemoryReportingDbContext(DbContextOptions<InMemoryReportingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportExecution> ReportExecutions => Set<ReportExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportDefinition>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasMany(x => x.Parameters)
                .WithOne()
                .HasForeignKey(p => p.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.DataSources)
                .WithOne()
                .HasForeignKey(ds => ds.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportDataSource>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportParameter>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportExecution>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasMany(x => x.OutputFiles)
                .WithOne()
                .HasForeignKey(f => f.ReportExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Ignore(x => x.RequestedFormats);
        });

        modelBuilder.Entity<ReportOutputFile>(b =>
        {
            b.HasKey(x => x.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
