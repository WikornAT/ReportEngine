using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SubCategory)
            .HasMaxLength(100);

        builder.Property(x => x.TemplatePath)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.IsHidden)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(256);

        builder.HasMany(x => x.Parameters)
            .WithOne()
            .HasForeignKey(p => p.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DataSources)
            .WithOne()
            .HasForeignKey(ds => ds.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Category, x.Name })
            .IsUnique();
    }
}
