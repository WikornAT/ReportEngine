using Reporting.Domain.Enums;

namespace Reporting.Application.DTOs;

/// <summary>
/// Read-model DTO for a <see cref="Domain.ReportDefinitions.ReportParameter"/>.
/// Surfaced in <see cref="ReportDefinitionDto.Parameters"/>.
/// </summary>
public sealed record ReportParameterDto(
    Guid Id,
    Guid ReportDefinitionId,
    string Name,
    string DisplayName,
    string? Description,
    ReportParameterType ParameterType,
    bool IsRequired,
    string? DefaultValue,
    int SortOrder,
    bool IsVisible);
