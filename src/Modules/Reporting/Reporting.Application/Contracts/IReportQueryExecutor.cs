namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for executing a report's data source queries and returning raw tabular data.
/// <para>
/// Implementations live in <c>Reporting.Infrastructure</c> and translate each
/// <see cref="Domain.ReportDefinitions.ReportDataSource"/> into ADO.NET calls
/// based on the <see cref="Domain.Enums.ReportDataSourceType"/>.
/// </para>
/// <para>
/// <b>Security:</b> SQL text is <em>never</em> accepted from API callers.
/// Only <see cref="Domain.ReportDefinitions.ReportDataSource.QueryText"/> values
/// stored in the database are executed.
/// Runtime values are supplied as named parameters only.
/// </para>
/// </summary>
public interface IReportQueryExecutor
{
    /// <summary>
    /// Executes all active data source queries declared on the given report definition.
    /// </summary>
    /// <param name="reportDefinitionId">
    /// The id of the report definition whose data sources will be queried.
    /// </param>
    /// <param name="parametersJson">
    /// JSON object containing the resolved parameter values, keyed by parameter name.
    /// </param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>
    /// A <see cref="DataSourceExecutionResult"/> containing JSON keyed by
    /// <c>_data_{sourceName}</c> and per-source execution metrics.
    /// </returns>
    Task<DataSourceExecutionResult> ExecuteAsync(
        Guid reportDefinitionId,
        string parametersJson,
        CancellationToken cancellationToken = default);
}
