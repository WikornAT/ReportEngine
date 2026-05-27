using Reporting.Domain.Enums;

namespace Reporting.Application.DTOs;

/// <summary>
/// Read-model DTO for a <see cref="Domain.ReportDefinitions.ReportDefinition"/> aggregate.
/// Returned by queries and command results.
/// </summary>
public sealed record ReportDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    string? SubCategory,
    Guid? TemplateId,
    string? TemplatePath,
    ReportStatus Status,
    bool IsHidden,
    int? ExecutionTimeoutSeconds,
    int? MaxRowCount,
    IReadOnlyList<ReportParameterDto> Parameters,
    IReadOnlyList<ReportDataSourceDto> DataSources,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? ModifiedAt,
    string? ModifiedBy);
