using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportDefinitions;

/// <summary>
/// Describes a data source bound to a <see cref="ReportDefinition"/>.
/// <para>
/// A single report definition may reference multiple data sources (e.g., a main dataset
/// and a sub-report dataset).  Each <see cref="ReportDataSource"/> is an owned child entity
/// and must only be created/mutated through the <see cref="ReportDefinition"/> aggregate root.
/// </para>
/// <para>
/// <b>Security:</b> <see cref="ConnectionStringName"/> is an <em>indirect</em> reference to a
/// named connection string stored in a secrets vault or configuration provider — never a literal
/// connection string.  The infrastructure layer resolves it at runtime.
/// </para>
/// <para>
/// <b>Extension point:</b> Add <c>TimeoutSeconds</c> (int), <c>CacheDurationSeconds</c> (int?),
/// and <c>AuthenticationScheme</c> (string?) as the engine matures.
/// </para>
/// </summary>
public sealed class ReportDataSource
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Foreign key to the owning <see cref="ReportDefinition"/>.</summary>
    public Guid ReportDefinitionId { get; private set; }

    // ── Descriptor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Logical name that identifies this data source within the report template
    /// (e.g., <c>MainDataset</c>, <c>LookupCurrencies</c>).
    /// Must be unique within the owning report definition.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The technology/protocol used to retrieve data.
    /// Determines how <see cref="QueryText"/> and <see cref="ConnectionStringName"/> are interpreted.
    /// </summary>
    public ReportDataSourceType DataSourceType { get; private set; }

    // ── Connection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Indirect reference to a named connection string registered in the application's
    /// configuration or secrets vault.  <b>Must not</b> contain a literal connection string.
    /// </summary>
    public string ConnectionStringName { get; private set; } = string.Empty;

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The query to execute against the data source.
    /// Semantics depend on <see cref="DataSourceType"/>:
    /// <list type="bullet">
    ///   <item><term>SqlQuery</term><description>A SQL SELECT statement.</description></item>
    ///   <item><term>StoredProcedure</term><description>Stored procedure name (schema-qualified).</description></item>
    ///   <item><term>WebService</term><description>Relative endpoint path or operation name.</description></item>
    ///   <item><term>Json / Xml</term><description>JSONPath / XPath selector or static payload.</description></item>
    ///   <item><term>OData</term><description>OData query string options.</description></item>
    ///   <item><term>InMemory / Custom</term><description>Type name or key registered with the engine.</description></item>
    /// </list>
    /// </summary>
    public string QueryText { get; private set; } = string.Empty;

    /// <summary>
    /// Display order when multiple data sources are listed in the designer/UI.
    /// Lower values appear first.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Query execution timeout in seconds.
    /// <see langword="null"/> means the default database timeout is used.
    /// </summary>
    public int? TimeoutSeconds { get; private set; }

    /// <summary>
    /// When <see langword="false"/>, this data source is skipped during execution.
    /// Useful for disabling a source without deleting it.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    // ── Parameters ────────────────────────────────────────────────────────────

    private readonly List<ReportDataSourceParameter> _parameters = [];

    /// <summary>
    /// Parameter mappings that bind <see cref="ReportParameter"/> runtime values to the
    /// query/stored-procedure parameters declared in <see cref="QueryText"/>.
    /// </summary>
    public IReadOnlyList<ReportDataSourceParameter> Parameters => _parameters.AsReadOnly();

    // ── ORM constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Private parameterless constructor required by EF Core.
    /// Do not use directly; use <see cref="Create"/> instead.
    /// </summary>
    private ReportDataSource() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new, valid <see cref="ReportDataSource"/> child entity.
    /// Called exclusively by <see cref="ReportDefinition.AddDataSource"/>.
    /// </summary>
    /// <param name="reportDefinitionId">Id of the owning aggregate root.</param>
    /// <param name="name">Logical data source name within the report (non-empty, max 100 chars).</param>
    /// <param name="dataSourceType">Connection/query technology type.</param>
    /// <param name="connectionStringName">Named connection string reference (non-empty).</param>
    /// <param name="queryText">Query, SP name, or endpoint (non-empty).</param>
    /// <param name="sortOrder">Display order (must be &gt; 0).</param>
    /// <param name="timeoutSeconds">Optional query timeout in seconds.</param>
    /// <param name="isActive">Whether this source participates in execution. Defaults to <see langword="true"/>.</param>
    /// <returns>A new <see cref="ReportDataSource"/> instance.</returns>
    internal static ReportDataSource Create(
        Guid reportDefinitionId,
        string name,
        ReportDataSourceType dataSourceType,
        string connectionStringName,
        string queryText,
        int sortOrder,
        int? timeoutSeconds = null,
        bool isActive = true)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.DefinedEnum(dataSourceType, nameof(dataSourceType));

        if (RequiresConnectionString(dataSourceType))
        {
            Guard.NotNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));
        }

        if (RequiresQueryText(dataSourceType))
        {
            Guard.NotNullOrWhiteSpace(queryText, nameof(queryText));
        }

        if (name.Length > 100)
        {
            throw new ReportingDomainException($"'{nameof(name)}' must not exceed 100 characters.");
        }

        return new ReportDataSource
        {
            Id = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            Name = name,
            DataSourceType = dataSourceType,
            ConnectionStringName = connectionStringName,
            QueryText = queryText,
            SortOrder = sortOrder,
            TimeoutSeconds = timeoutSeconds,
            IsActive = isActive,
        };
    }

    // ── Domain behaviour ──────────────────────────────────────────────────────

    /// <summary>
    /// Updates the query text for this data source.
    /// Use when the report designer modifies the underlying SQL or endpoint path.
    /// </summary>
    /// <param name="queryText">New query text (non-empty).</param>
    internal void UpdateQueryText(string queryText)
    {
        if (RequiresQueryText(DataSourceType))
        {
            Guard.NotNullOrWhiteSpace(queryText, nameof(queryText));
        }
        QueryText = queryText;
    }

    /// <summary>
    /// Re-assigns the named connection string reference.
    /// Useful when migrating a report between database environments.
    /// </summary>
    /// <param name="connectionStringName">New named connection string reference (non-empty).</param>
    internal void UpdateConnectionStringName(string connectionStringName)
    {
        if (RequiresConnectionString(DataSourceType))
        {
            Guard.NotNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));
        }
        ConnectionStringName = connectionStringName;
    }

    /// <summary>
    /// Updates all mutable fields of this data source in a single atomic operation.
    /// </summary>
    /// <param name="name">New logical name (non-empty, max 100 chars).</param>
    /// <param name="dataSourceType">New connection/query technology type.</param>
    /// <param name="connectionStringName">New named connection string reference (non-empty).</param>
    /// <param name="queryText">New query, SP name, or endpoint (non-empty).</param>
    /// <param name="sortOrder">New display order.</param>
    /// <param name="timeoutSeconds">Optional query timeout in seconds.</param>
    /// <param name="isActive">Whether this source participates in execution.</param>
    internal void Update(
        string name,
        ReportDataSourceType dataSourceType,
        string connectionStringName,
        string queryText,
        int sortOrder,
        int? timeoutSeconds = null,
        bool isActive = true)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.DefinedEnum(dataSourceType, nameof(dataSourceType));

        if (RequiresConnectionString(dataSourceType))
        {
            Guard.NotNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));
        }

        if (RequiresQueryText(dataSourceType))
        {
            Guard.NotNullOrWhiteSpace(queryText, nameof(queryText));
        }

        if (name.Length > 100)
        {
            throw new ReportingDomainException($"'{nameof(name)}' must not exceed 100 characters.");
        }

        Name = name;
        DataSourceType = dataSourceType;
        ConnectionStringName = connectionStringName;
        QueryText = queryText;
        SortOrder = sortOrder;
        TimeoutSeconds = timeoutSeconds;
        IsActive = isActive;
    }

    /// <summary>
    /// Adds a new parameter mapping to this data source.
    /// </summary>
    internal void AddParameter(
        string sourceParameterName,
        string reportParameterName,
        string? dbType,
        bool isRequired,
        string? defaultValue)
    {
        _parameters.Add(ReportDataSourceParameter.Create(
            Id, sourceParameterName, reportParameterName, dbType, isRequired, defaultValue));
    }

    /// <summary>
    /// Removes the parameter mapping with the specified <paramref name="parameterId"/>.
    /// </summary>
    internal void RemoveParameter(Guid parameterId)
    {
        int index = _parameters.FindIndex(p => p.Id == parameterId);
        if (index >= 0)
        {
            _parameters.RemoveAt(index);
        }
    }

    /// <summary>
    /// Updates the parameter mapping with the specified <paramref name="parameterId"/>.
    /// Throws <see cref="ReportingDomainException"/> if the mapping is not found.
    /// </summary>
    internal void UpdateParameter(
        Guid parameterId,
        string sourceParameterName,
        string reportParameterName,
        string? dbType,
        bool isRequired,
        string? defaultValue)
    {
        ReportDataSourceParameter? param = _parameters.Find(p => p.Id == parameterId);
        if (param is null)
        {
            throw new ReportingDomainException(
                $"Parameter mapping '{parameterId}' not found on data source '{Name}'.");
        }

        param.Update(sourceParameterName, reportParameterName, dbType, isRequired, defaultValue);
    }

    /// <summary>
    /// Returns <see langword="true"/> for data source types that require a non-empty
    /// <c>queryText</c> (SQL and stored procedures).
    /// Non-database types such as <see cref="ReportDataSourceType.Json"/>,
    /// <see cref="ReportDataSourceType.InMemory"/>, and
    /// <see cref="ReportDataSourceType.WebService"/> supply data through other means.
    /// </summary>
    private static bool RequiresQueryText(ReportDataSourceType type) =>
        type is ReportDataSourceType.SqlQuery or ReportDataSourceType.StoredProcedure;

    /// <summary>
    /// Returns <see langword="true"/> for data source types that require a database
    /// connection string. Non-database types (Json, InMemory, WebService, etc.) source
    /// their data without a connection string.
    /// </summary>
    public static bool RequiresConnectionString(ReportDataSourceType type) =>
        type is ReportDataSourceType.SqlQuery or ReportDataSourceType.StoredProcedure;
}
