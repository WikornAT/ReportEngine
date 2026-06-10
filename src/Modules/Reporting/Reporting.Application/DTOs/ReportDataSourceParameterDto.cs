namespace Reporting.Application.DTOs;

/// <summary>
/// Read-model DTO for a <see cref="Domain.ReportDefinitions.ReportDataSourceParameter"/>.
/// Surfaced in <see cref="ReportDataSourceDto.Parameters"/>.
/// </summary>
public sealed record ReportDataSourceParameterDto(
    Guid Id,
    Guid ReportDataSourceId,
    string SourceParameterName,
    string ReportParameterName,
    string? DbType,
    bool IsRequired,
    string? DefaultValue);
