using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.ReportDefinitions;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportParameterConfiguration : IEntityTypeConfiguration<ReportParameter>
{
    public void Configure(EntityTypeBuilder<ReportParameter> builder)
    {
        builder.ToTable("report_parameters");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportDefinitionId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.ParameterType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.IsRequired)
            .IsRequired();

        builder.Property(x => x.DefaultValue)
            .HasMaxLength(500);

        builder.Property(x => x.IsVisible)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.ReportDefinitionId, x.Name })
            .IsUnique();
    }
}
