using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.Enums;
using Reporting.Domain.RenderLogs;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class RenderLogConfiguration : IEntityTypeConfiguration<RenderLog>
{
    public void Configure(EntityTypeBuilder<RenderLog> builder)
    {
        builder.ToTable("render_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReportDefinitionId).IsRequired();
        builder.Property(x => x.TemplateId);

        builder.Property(x => x.Format)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.CompletedAt);
        builder.Property(x => x.DurationMs);
        builder.Property(x => x.OutputSizeBytes);

        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);

        builder.Property(x => x.TriggeredBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.ReportDefinitionId);
        builder.HasIndex(x => x.StartedAt);
    }
}
