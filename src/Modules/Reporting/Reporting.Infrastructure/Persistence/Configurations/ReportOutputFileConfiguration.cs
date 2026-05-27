using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reporting.Domain.ReportExecutions;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportOutputFileConfiguration : IEntityTypeConfiguration<ReportOutputFile>
{
    public void Configure(EntityTypeBuilder<ReportOutputFile> builder)
    {
        builder.ToTable("report_output_files");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportExecutionId)
            .IsRequired();

        builder.Property(x => x.OutputFormat)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.ReportExecutionId);
    }
}
