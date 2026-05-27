using Reporting.Domain.Enums;

namespace Reporting.Application.DTOs;

/// <summary>
/// Read-model DTO for a <see cref="Domain.ReportExecutions.ReportExecution"/> aggregate.
/// Returned by execution commands and queries.
/// </summary>
public sealed record ReportExecutionDto(
    Guid Id,
    Guid ReportDefinitionId,
    string ReportName,
    string ParametersJson,
    IReadOnlyList<ReportOutputFormat> RequestedFormats,
    ReportExecutionStatus Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    string? ErrorMessage,
    int? RowCount,
    string TriggeredBy,
    string? CorrelationId,
    IReadOnlyList<ReportOutputFileDto> OutputFiles,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? ModifiedAt,
    string? ModifiedBy);
