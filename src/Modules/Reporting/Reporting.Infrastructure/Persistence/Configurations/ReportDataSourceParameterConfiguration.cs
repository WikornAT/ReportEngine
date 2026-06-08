using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.ReportDefinitions;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportDataSourceParameterConfiguration : IEntityTypeConfiguration<ReportDataSourceParameter>
{
    public void Configure(EntityTypeBuilder<ReportDataSourceParameter> builder)
    {
        builder.ToTable("report_data_source_parameters");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportDataSourceId)
            .IsRequired();

        builder.Property(x => x.SourceParameterName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ReportParameterName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.DbType)
            .HasMaxLength(50);

        builder.Property(x => x.IsRequired)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DefaultValue)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.ReportDataSourceId, x.SourceParameterName })
            .IsUnique();
    }
}
