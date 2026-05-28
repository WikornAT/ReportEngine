using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.Activate;

/// <summary>
/// Publishes a <see cref="Domain.ReportDefinitions.ReportDefinition"/>, transitioning it
/// from Draft or Inactive to Active so it can be executed.
/// The report must have at least one data source configured.
/// </summary>
/// <param name="Id">The id of the report definition to activate.</param>
public sealed record ActivateReportDefinitionCommand(Guid Id) : IRequest<Result<ReportDefinitionDto>>;
