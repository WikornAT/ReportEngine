using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Templates.Domain.ReportTemplates;

namespace Templates.Infrastructure.Persistence.Configurations;

internal sealed class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.ToTable("template_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TemplateId).IsRequired();
        builder.Property(x => x.Version).IsRequired();

        builder.Property(x => x.HtmlContent)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.CssContent)
            .HasColumnType("text");

        builder.Property(x => x.SnapshotJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.TemplateId);
        builder.HasIndex(x => new { x.TemplateId, x.Version }).IsUnique();
    }
}
