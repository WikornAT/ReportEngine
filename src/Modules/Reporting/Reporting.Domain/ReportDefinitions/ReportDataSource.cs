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
    /// <returns>A new <see cref="ReportDataSource"/> instance.</returns>
    internal static ReportDataSource Create(
        Guid reportDefinitionId,
        string name,
        ReportDataSourceType dataSourceType,
        string connectionStringName,
        string queryText,
        int sortOrder)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));
        Guard.NotNullOrWhiteSpace(queryText, nameof(queryText));
        Guard.DefinedEnum(dataSourceType, nameof(dataSourceType));

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
        Guard.NotNullOrWhiteSpace(queryText, nameof(queryText));
        QueryText = queryText;
    }

    /// <summary>
    /// Re-assigns the named connection string reference.
    /// Useful when migrating a report between database environments.
    /// </summary>
    /// <param name="connectionStringName">New named connection string reference (non-empty).</param>
    internal void UpdateConnectionStringName(string connectionStringName)
    {
        Guard.NotNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));
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
    internal void Update(
        string name,
        ReportDataSourceType dataSourceType,
        string connectionStringName,
        string queryText,
        int sortOrder)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrWhiteSpace(connectionStringName, nameof(connectionStringName));
        Guard.NotNullOrWhiteSpace(queryText, nameof(queryText));
        Guard.DefinedEnum(dataSourceType, nameof(dataSourceType));

        if (name.Length > 100)
        {
            throw new ReportingDomainException($"'{nameof(name)}' must not exceed 100 characters.");
        }

        Name = name;
        DataSourceType = dataSourceType;
        ConnectionStringName = connectionStringName;
        QueryText = queryText;
        SortOrder = sortOrder;
    }
}
