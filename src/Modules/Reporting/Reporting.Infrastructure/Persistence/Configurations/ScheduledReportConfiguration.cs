using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.Enums;
using Reporting.Domain.ReportSchedules;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("scheduled_reports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportDefinitionId)
            .IsRequired();

        builder.Property(x => x.ScheduleType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ParametersJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.RequestedFormatsCsv)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(x => x.TriggeredBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<Reporting.Domain.ReportDefinitions.ReportDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.ScheduleType);
        builder.HasIndex(x => new { x.ReportDefinitionId, x.ScheduleType });
    }
}
