using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.GetDataSources;

/// <summary>
/// Returns all data sources belonging to a <see cref="Domain.ReportDefinitions.ReportDefinition"/>,
/// ordered by <c>SortOrder</c> ascending.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
public sealed record GetReportDataSourcesQuery(
    Guid ReportDefinitionId) : IRequest<Result<IReadOnlyList<ReportDataSourceDto>>>;
