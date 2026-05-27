using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.ReportDefinitions;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportDataSourceConfiguration : IEntityTypeConfiguration<ReportDataSource>
{
    public void Configure(EntityTypeBuilder<ReportDataSource> builder)
    {
        builder.ToTable("report_data_sources");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportDefinitionId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.DataSourceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ConnectionStringName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.QueryText)
            .IsRequired();

        builder.HasIndex(x => new { x.ReportDefinitionId, x.Name })
            .IsUnique();
    }
}
