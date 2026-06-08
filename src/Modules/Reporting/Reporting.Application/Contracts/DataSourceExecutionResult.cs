namespace Reporting.Application.Contracts;

/// <summary>
/// The outcome of executing all active data sources for a report.
/// </summary>
/// <param name="DataJson">
/// JSON object whose keys follow the <c>_data_{sourceName}</c> convention consumed by
/// <c>HtmlReportRenderer.BuildRenderContext</c>. Each value is a JSON array of row objects.
/// </param>
/// <param name="DataSourceExecutionMs">Total wall-clock time spent executing all data sources.</param>
/// <param name="SourceMetrics">Per-source execution metrics (name, row count, elapsed ms, optional error).</param>
public sealed record DataSourceExecutionResult(
    string DataJson,
    long DataSourceExecutionMs,
    IReadOnlyList<DataSourceMetric> SourceMetrics);

/// <summary>Per-source execution metric captured during a query run.</summary>
/// <param name="SourceName">The logical data source name.</param>
/// <param name="RowCount">Number of rows returned (0 on error).</param>
/// <param name="ElapsedMs">Wall-clock time for this source in milliseconds.</param>
/// <param name="ErrorMessage">Error description if the source failed; <see langword="null"/> on success.</param>
public sealed record DataSourceMetric(
    string SourceName,
    int RowCount,
    long ElapsedMs,
    string? ErrorMessage);
