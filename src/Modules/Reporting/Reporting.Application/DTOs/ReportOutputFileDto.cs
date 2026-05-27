using Reporting.Domain.Enums;

namespace Reporting.Application.DTOs;

/// <summary>
/// Read-model DTO for a <see cref="Domain.ReportExecutions.ReportOutputFile"/>.
/// Surfaced in <see cref="ReportExecutionDto.OutputFiles"/>.
/// </summary>
public sealed record ReportOutputFileDto(
    Guid Id,
    Guid ReportExecutionId,
    ReportOutputFormat OutputFormat,
    string FileName,
    string StoragePath,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset GeneratedAt);
