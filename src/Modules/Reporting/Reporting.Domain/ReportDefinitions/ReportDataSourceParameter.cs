using Reporting.Domain.Common;

namespace Reporting.Domain.ReportDefinitions;

/// <summary>
/// Maps a declared <see cref="ReportParameter"/> name to the corresponding database
/// parameter used in a <see cref="ReportDataSource"/> query or stored procedure.
/// <para>
/// Owned child of <see cref="ReportDataSource"/>; must only be created/mutated through
/// the parent aggregate root.
/// </para>
/// </summary>
public sealed class ReportDataSourceParameter
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Foreign key to the owning <see cref="ReportDataSource"/>.</summary>
    public Guid ReportDataSourceId { get; private set; }

    // ── Mapping ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The parameter name as declared in the SQL query or stored procedure
    /// (e.g., <c>start_date</c> for <c>WHERE created_at >= @start_date</c>).
    /// </summary>
    public string SourceParameterName { get; private set; } = string.Empty;

    /// <summary>
    /// The name of the <see cref="ReportParameter"/> on the parent
    /// <see cref="ReportDefinition"/> whose runtime value will be bound
    /// to <see cref="SourceParameterName"/>.
    /// </summary>
    public string ReportParameterName { get; private set; } = string.Empty;

    /// <summary>
    /// ADO.NET DB type hint string (e.g., <c>Date</c>, <c>Int32</c>, <c>VarChar</c>).
    /// When <see langword="null"/>, the executor infers the type from the runtime value.
    /// </summary>
    public string? DbType { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the executor throws if no value can be resolved for this parameter.
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Default value used when the corresponding report parameter was not supplied.
    /// Serialised as a string; the executor coerces to <see cref="DbType"/>.
    /// </summary>
    public string? DefaultValue { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    private ReportDataSourceParameter() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new parameter mapping for a <see cref="ReportDataSource"/>.
    /// Called exclusively through <see cref="ReportDataSource.AddParameter"/>.
    /// </summary>
    internal static ReportDataSourceParameter Create(
        Guid reportDataSourceId,
        string sourceParameterName,
        string reportParameterName,
        string? dbType,
        bool isRequired,
        string? defaultValue)
    {
        Guard.NotNullOrWhiteSpace(sourceParameterName, nameof(sourceParameterName));
        Guard.NotNullOrWhiteSpace(reportParameterName, nameof(reportParameterName));

        return new ReportDataSourceParameter
        {
            Id = Guid.NewGuid(),
            ReportDataSourceId = reportDataSourceId,
            SourceParameterName = sourceParameterName,
            ReportParameterName = reportParameterName,
            DbType = dbType,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
        };
    }

    // ── Domain behaviour ──────────────────────────────────────────────────────

    /// <summary>Updates all mutable fields of this parameter mapping.</summary>
    internal void Update(
        string sourceParameterName,
        string reportParameterName,
        string? dbType,
        bool isRequired,
        string? defaultValue)
    {
        Guard.NotNullOrWhiteSpace(sourceParameterName, nameof(sourceParameterName));
        Guard.NotNullOrWhiteSpace(reportParameterName, nameof(reportParameterName));

        SourceParameterName = sourceParameterName;
        ReportParameterName = reportParameterName;
        DbType = dbType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }
}
