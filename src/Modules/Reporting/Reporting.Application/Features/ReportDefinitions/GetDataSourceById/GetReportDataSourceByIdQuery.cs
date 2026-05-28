using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.GetDataSourceById;

/// <summary>
/// Returns a single data source by its id within a <see cref="Domain.ReportDefinitions.ReportDefinition"/>.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
/// <param name="DataSourceId">The data source to retrieve.</param>
public sealed record GetReportDataSourceByIdQuery(
    Guid ReportDefinitionId,
    Guid DataSourceId) : IRequest<Result<ReportDataSourceDto>>;
