using Exim.T4d.Labeling.Domain.GuaranteeDebt;
using Exim.T4d.Labeling.Domain.GuaranteeInfo;
using Labeling.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Labeling.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Labeling module.
/// Covers the T4D_DEV schema tables: guarantee_info and guarantee_debt.
/// </summary>
internal sealed class LabelingDbContext : DbContext
{
    public LabelingDbContext(DbContextOptions<LabelingDbContext> options)
        : base(options)
    {
    }

    public DbSet<GuaranteeInfo> GuaranteeInfos => Set<GuaranteeInfo>();
    public DbSet<GuaranteeDebt> GuaranteeDebts => Set<GuaranteeDebt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new GuaranteeInfoConfiguration());
        modelBuilder.ApplyConfiguration(new GuaranteeDebtConfiguration());
    }
}
