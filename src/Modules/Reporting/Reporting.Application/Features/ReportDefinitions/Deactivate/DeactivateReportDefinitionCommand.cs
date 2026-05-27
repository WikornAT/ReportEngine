using MediatR;

using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.Deactivate;

/// <summary>
/// Deactivates a <see cref="Domain.ReportDefinitions.ReportDefinition"/>, preventing
/// new executions while preserving historical execution data.
/// </summary>
/// <param name="Id">The id of the report definition to deactivate.</param>
public sealed record DeactivateReportDefinitionCommand(Guid Id) : IRequest<Result<ReportDefinitionDto>>;
