namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for executing a report's data source queries and returning raw tabular data.
/// <para>
/// Implementations live in <c>Reporting.Infrastructure</c> and translate each
/// <see cref="Domain.ReportDefinitions.ReportDataSource"/> into ADO.NET / EF Core / HTTP
/// calls based on the <see cref="Domain.Enums.ReportDataSourceType"/>.
/// </para>
/// </summary>
public interface IReportQueryExecutor
{
    /// <summary>
    /// Executes all data source queries declared on the given report definition and returns
    /// the combined result as a serialized JSON payload.
    /// </summary>
    /// <param name="reportDefinitionId">
    /// The id of the <see cref="Domain.ReportDefinitions.ReportDefinition"/> whose data sources
    /// will be queried.
    /// </param>
    /// <param name="parametersJson">
    /// JSON object containing the resolved parameter values, keyed by parameter name.
    /// </param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>
    /// A JSON string containing the query result sets, keyed by data source name.
    /// </returns>
    public Task<string> ExecuteAsync(
        Guid reportDefinitionId,
        string parametersJson,
        CancellationToken cancellationToken = default);
}
