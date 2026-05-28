using MediatR;

using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RemoveDataSource;

/// <summary>
/// Removes a data source from an existing <see cref="Domain.ReportDefinitions.ReportDefinition"/>.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
/// <param name="DataSourceId">The data source to remove.</param>
public sealed record RemoveReportDataSourceCommand(
    Guid ReportDefinitionId,
    Guid DataSourceId) : IRequest<Result<Unit>>;
