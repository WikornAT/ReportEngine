using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Templates.Domain.ReportTemplates;

namespace Templates.Infrastructure.Persistence.Configurations;

internal sealed class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> builder)
    {
        builder.ToTable("report_templates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        // Store HTML as unbounded text (PostgreSQL text column)
        builder.Property(x => x.HtmlContent)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.CssContent)
            .HasColumnType("text");

        builder.Property(x => x.PaperSize)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Orientation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.WidthPx).IsRequired();
        builder.Property(x => x.HeightPx).IsRequired();
        builder.Property(x => x.Version).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(256);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
