using MediatR;

using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportDefinitions.AddParameter;

/// <summary>
/// Declares a new input parameter on an existing <see cref="Domain.ReportDefinitions.ReportDefinition"/>.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
/// <param name="Name">Binding token (e.g., <c>StartDate</c>) — unique within the report.</param>
/// <param name="DisplayName">UI label shown in the report runner.</param>
/// <param name="ParameterType">Data type for binding and validation.</param>
/// <param name="IsRequired">Whether the parameter must be supplied at execution time.</param>
/// <param name="DefaultValue">Optional serialized default value.</param>
/// <param name="SortOrder">Display order in the runner UI.</param>
/// <param name="IsVisible">Whether to surface the parameter in the UI.</param>
/// <param name="Description">Optional tooltip text.</param>
public sealed record AddReportParameterCommand(
    Guid ReportDefinitionId,
    string Name,
    string DisplayName,
    ReportParameterType ParameterType,
    bool IsRequired,
    string? DefaultValue,
    int SortOrder,
    bool IsVisible,
    string? Description) : IRequest<Result<ReportDefinitionDto>>;
