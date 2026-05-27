using MediatR;

using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.GetById;

/// <summary>
/// Returns a single <see cref="Domain.ReportDefinitions.ReportDefinition"/> by its id,
/// including all child parameters and data sources.
/// </summary>
/// <param name="Id">The report definition id to look up.</param>
public sealed record GetReportDefinitionByIdQuery(Guid Id) : IRequest<Result<ReportDefinitionDto>>;
