using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Reporting.Domain.Enums;
using Reporting.Domain.ReportExecutions;

namespace Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportExecutionConfiguration : IEntityTypeConfiguration<ReportExecution>
{
    public void Configure(EntityTypeBuilder<ReportExecution> builder)
    {
        builder.ToTable("report_executions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportDefinitionId)
            .IsRequired();

        builder.Property(x => x.ReportName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ParametersJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.TriggeredBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(4000);

        // RequestedFormats: List<ReportOutputFormat> stored as a JSON array in a jsonb column.
        // EF accesses the private backing field _requestedFormats via Field access mode.
        var formatsConverter = new ValueConverter<List<ReportOutputFormat>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<ReportOutputFormat>>(v, (JsonSerializerOptions?)null) ?? new List<ReportOutputFormat>());

        builder.Property<List<ReportOutputFormat>>("_requestedFormats")
            .HasField("_requestedFormats")
            .HasColumnName("requested_formats")
            .HasColumnType("jsonb")
            .HasConversion(formatsConverter)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.HasMany(x => x.OutputFiles)
            .WithOne()
            .HasForeignKey(f => f.ReportExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ReportDefinitionId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TriggeredBy);
        builder.HasIndex(x => x.CreatedAt);
    }
}
