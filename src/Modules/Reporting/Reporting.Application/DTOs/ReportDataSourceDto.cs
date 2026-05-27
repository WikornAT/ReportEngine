using Reporting.Domain.Enums;

namespace Reporting.Application.DTOs;

/// <summary>
/// Read-model DTO for a <see cref="Domain.ReportDefinitions.ReportDataSource"/>.
/// Surfaced in <see cref="ReportDefinitionDto.DataSources"/>.
/// </summary>
public sealed record ReportDataSourceDto(
    Guid Id,
    Guid ReportDefinitionId,
    string Name,
    ReportDataSourceType DataSourceType,
    string ConnectionStringName,
    string QueryText,
    int SortOrder);
