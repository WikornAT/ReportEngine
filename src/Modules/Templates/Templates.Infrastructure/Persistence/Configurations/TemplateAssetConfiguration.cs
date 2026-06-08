using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Templates.Domain.ReportTemplates;

namespace Templates.Infrastructure.Persistence.Configurations;

internal sealed class TemplateAssetConfiguration : IEntityTypeConfiguration<TemplateAsset>
{
    public void Configure(EntityTypeBuilder<TemplateAsset> builder)
    {
        builder.ToTable("template_assets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TemplateId).IsRequired();

        builder.Property(x => x.AssetType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.RelativePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.StoragePath)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.PublicUrl)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.SizeBytes).IsRequired();

        builder.Property(x => x.Sha256Hash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.TemplateId);
        builder.HasIndex(x => new { x.TemplateId, x.RelativePath }).IsUnique();
    }
}
