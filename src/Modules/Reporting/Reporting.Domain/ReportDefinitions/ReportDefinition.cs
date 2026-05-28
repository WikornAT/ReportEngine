using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportDefinitions;

/// <summary>
/// Aggregate root representing the design-time definition of a report.
/// <para>
/// A <see cref="ReportDefinition"/> is the central artifact of the reporting module.
/// It describes <em>what</em> a report is — its metadata, layout template reference,
/// data sources, and accepted parameters — but does not hold execution state.
/// Execution state lives in <see cref="ReportExecutions.ReportExecution"/>.
/// </para>
/// <para>
/// <b>Aggregate boundary:</b>
/// <list type="bullet">
///   <item><see cref="ReportParameter"/> — owned child, mutated via <see cref="AddParameter"/>,
///   <see cref="UpdateParameterMetadata"/>, <see cref="RemoveParameter"/>.</item>
///   <item><see cref="ReportDataSource"/> — owned child, mutated via <see cref="AddDataSource"/>,
///   <see cref="UpdateDataSourceQuery"/>, <see cref="RemoveDataSource"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Invariants enforced:</b>
/// <list type="bullet">
///   <item>Name and Category must be non-empty.</item>
///   <item>Parameter names must be unique within the definition.</item>
///   <item>Data source names must be unique within the definition.</item>
///   <item>Only <see cref="ReportStatus.Active"/> reports may be executed.</item>
///   <item>Archived reports are immutable.</item>
/// </list>
/// </para>
/// <para>
/// <b>Extension points:</b> Add <c>TemplateId</c> (Guid?) when the Templates module is integrated;
/// add <c>Tags</c> (IReadOnlyList&lt;string&gt;) for search/filtering; add <c>OwnerId</c> (Guid)
/// for row-level security.
/// </para>
/// </summary>
public sealed class ReportDefinition : IAuditableEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    // ── Descriptor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Human-readable name of the report (non-empty, max 200 characters).
    /// Shown in the report catalogue and runner.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional detailed description of the report's purpose and output.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Logical category or module grouping (e.g., <c>Finance</c>, <c>Operations</c>).
    /// Used for navigation, access control, and filtering.
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Optional sub-category within <see cref="Category"/> for finer-grained grouping.
    /// </summary>
    public string? SubCategory { get; private set; }

    // ── Template Reference ────────────────────────────────────────────────────

    /// <summary>
    /// Reference to the layout template identifier in the Templates module.
    /// <see langword="null"/> until a template is associated.
    /// <b>Extension point:</b> Typed cross-module reference — resolved at the Application layer.
    /// </summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>
    /// Physical file path or storage key of the report template (e.g., RDLC, Crystal RPT).
    /// The infrastructure layer uses this to locate the binary template at render time.
    /// </summary>
    public string? TemplatePath { get; private set; }

    // ── Status & Visibility ───────────────────────────────────────────────────

    /// <summary>Current lifecycle status of this report definition.</summary>
    public ReportStatus Status { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the report is hidden from catalogue listings
    /// but remains accessible by direct ID reference (useful for system/internal reports).
    /// </summary>
    public bool IsHidden { get; private set; }

    // ── Execution Constraints ─────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of seconds the engine waits before aborting a single execution.
    /// <see langword="null"/> means the global engine default is used.
    /// </summary>
    public int? ExecutionTimeoutSeconds { get; private set; }

    /// <summary>
    /// Maximum number of rows the engine will return in a single execution.
    /// Guards against runaway queries. <see langword="null"/> means no limit is applied.
    /// </summary>
    public int? MaxRowCount { get; private set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <inheritdoc/>
    public string? ModifiedBy { get; private set; }

    // ── Children ──────────────────────────────────────────────────────────────

    private readonly List<ReportParameter> _parameters = [];
    private readonly List<ReportDataSource> _dataSources = [];

    /// <summary>Input parameters declared for this report (ordered by <c>SortOrder</c>).</summary>
    public IReadOnlyList<ReportParameter> Parameters => _parameters.AsReadOnly();

    /// <summary>Data sources bound to this report.</summary>
    public IReadOnlyList<ReportDataSource> DataSources => _dataSources.AsReadOnly();

    // ── ORM constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Private parameterless constructor required by EF Core.
    /// Do not use directly; use <see cref="Create"/> instead.
    /// </summary>
    private ReportDefinition() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="ReportDefinition"/> in <see cref="ReportStatus.Draft"/> status.
    /// The report must be explicitly published via <see cref="Publish"/> before it can be executed.
    /// </summary>
    /// <param name="name">Report display name (non-empty, max 200 characters).</param>
    /// <param name="category">Logical category grouping (non-empty).</param>
    /// <param name="createdBy">Identity of the creator (non-empty).</param>
    /// <param name="description">Optional description.</param>
    /// <param name="subCategory">Optional sub-category.</param>
    /// <returns>A new <see cref="ReportDefinition"/> instance in Draft status.</returns>
    public static ReportDefinition Create(
        string name,
        string category,
        string createdBy,
        string? description = null,
        string? subCategory = null)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrWhiteSpace(category, nameof(category));
        Guard.NotNullOrWhiteSpace(createdBy, nameof(createdBy));

        if (name.Length > 200)
        {
            throw new ReportingDomainException($"'{nameof(name)}' must not exceed 200 characters.");
        }

        return new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Category = category,
            SubCategory = subCategory,
            Status = ReportStatus.Draft,
            IsHidden = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }

    // ── Lifecycle transitions ─────────────────────────────────────────────────

    /// <summary>
    /// Publishes the report, making it available for execution.
    /// </summary>
    /// <param name="modifiedBy">Identity performing the action.</param>
    /// <exception cref="ReportingDomainException">
    /// Thrown when the report is already Archived or when it has no data sources configured.
    /// </exception>
    public void Publish(string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        if (_dataSources.Count == 0)
        {
            throw new ReportingDomainException(
                "A report must have at least one data source before it can be published.");
        }

        Status = ReportStatus.Active;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Deactivates the report, preventing new executions while preserving history.
    /// </summary>
    /// <param name="modifiedBy">Identity performing the action.</param>
    /// <exception cref="ReportingDomainException">Thrown when the report is Archived.</exception>
    public void Deactivate(string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        Status = ReportStatus.Inactive;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Archives the report.  Archived reports are immutable and cannot be re-activated.
    /// </summary>
    /// <param name="modifiedBy">Identity performing the action.</param>
    /// <exception cref="ReportingDomainException">Thrown when the report is already Archived.</exception>
    public void Archive(string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        Status = ReportStatus.Archived;
        Touch(modifiedBy);
    }

    // ── Metadata mutations ────────────────────────────────────────────────────

    /// <summary>
    /// Updates the display metadata of the report definition.
    /// </summary>
    /// <param name="name">New display name (non-empty, max 200 characters).</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="category">New category (non-empty).</param>
    /// <param name="subCategory">New sub-category (optional).</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">Thrown when the report is Archived.</exception>
    public void UpdateMetadata(
        string name,
        string? description,
        string category,
        string? subCategory,
        string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrWhiteSpace(category, nameof(category));
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        if (name.Length > 200)
        {
            throw new ReportingDomainException($"'{nameof(name)}' must not exceed 200 characters.");
        }

        Name = name;
        Description = description;
        Category = category;
        SubCategory = subCategory;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Associates or replaces the layout template for this report.
    /// </summary>
    /// <param name="templateId">Reference to the template entity in the Templates module.</param>
    /// <param name="templatePath">Physical storage path or key of the template file.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">Thrown when the report is Archived.</exception>
    public void AssignTemplate(Guid templateId, string templatePath, string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(templatePath, nameof(templatePath));
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        TemplateId = templateId;
        TemplatePath = templatePath;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Sets execution constraints for this report.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum allowed execution time, or <see langword="null"/> for global default.</param>
    /// <param name="maxRowCount">Maximum result row count, or <see langword="null"/> for no limit.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">Thrown when the report is Archived or values are out of range.</exception>
    public void SetExecutionConstraints(int? timeoutSeconds, int? maxRowCount, string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        if (timeoutSeconds is <= 0)
        {
            throw new ReportingDomainException($"'{nameof(timeoutSeconds)}' must be a positive integer.");
        }

        if (maxRowCount is <= 0)
        {
            throw new ReportingDomainException($"'{nameof(maxRowCount)}' must be a positive integer.");
        }

        ExecutionTimeoutSeconds = timeoutSeconds;
        MaxRowCount = maxRowCount;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Controls the catalogue visibility of this report.
    /// </summary>
    /// <param name="isHidden"><see langword="true"/> to hide; <see langword="false"/> to show.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    public void SetVisibility(bool isHidden, string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        IsHidden = isHidden;
        Touch(modifiedBy);
    }

    // ── Parameter management ──────────────────────────────────────────────────

    /// <summary>
    /// Declares a new input parameter on this report definition.
    /// </summary>
    /// <param name="name">Binding token (must be unique within this report, non-empty, max 100 chars).</param>
    /// <param name="displayName">UI label (non-empty, max 200 chars).</param>
    /// <param name="parameterType">Data type.</param>
    /// <param name="isRequired">Whether the parameter must be supplied at execution time.</param>
    /// <param name="defaultValue">Optional default value string.</param>
    /// <param name="sortOrder">Display order.</param>
    /// <param name="isVisible">Whether to show in the runner UI.</param>
    /// <param name="description">Optional tooltip text.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <returns>The newly created <see cref="ReportParameter"/>.</returns>
    /// <exception cref="ReportingDomainException">
    /// Thrown when the report is Archived or a parameter with the same name already exists.
    /// </exception>
    public ReportParameter AddParameter(
        string name,
        string displayName,
        ReportParameterType parameterType,
        bool isRequired,
        string? defaultValue,
        int sortOrder,
        bool isVisible,
        string modifiedBy,
        string? description = null)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        if (_parameters.Exists(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ReportingDomainException(
                $"A parameter named '{name}' already exists on this report definition.");
        }

        ReportParameter parameter = ReportParameter.Create(
            Id, name, displayName, parameterType,
            isRequired, defaultValue, sortOrder, isVisible, description);

        _parameters.Add(parameter);
        Touch(modifiedBy);

        return parameter;
    }

    /// <summary>
    /// Updates the display metadata of an existing parameter.
    /// </summary>
    /// <param name="parameterId">Id of the parameter to update.</param>
    /// <param name="displayName">New UI label.</param>
    /// <param name="description">New tooltip text.</param>
    /// <param name="sortOrder">New display order.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">Thrown when the parameter is not found or the report is Archived.</exception>
    public void UpdateParameterMetadata(
        Guid parameterId,
        string displayName,
        string? description,
        int sortOrder,
        string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        ReportParameter parameter = FindParameterOrThrow(parameterId);
        parameter.UpdateDisplayMetadata(displayName, description, sortOrder);
        Touch(modifiedBy);
    }

    /// <summary>
    /// Removes a declared parameter from this report definition.
    /// </summary>
    /// <param name="parameterId">Id of the parameter to remove.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">Thrown when the parameter is not found or the report is Archived.</exception>
    public void RemoveParameter(Guid parameterId, string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        ReportParameter parameter = FindParameterOrThrow(parameterId);
        _parameters.Remove(parameter);
        Touch(modifiedBy);
    }

    // ── Data source management ────────────────────────────────────────────────

    /// <summary>
    /// Binds a new data source to this report definition.
    /// </summary>
    /// <param name="name">Logical name (must be unique within this report, non-empty, max 100 chars).</param>
    /// <param name="dataSourceType">Connection technology type.</param>
    /// <param name="connectionStringName">Named connection string reference.</param>
    /// <param name="queryText">SQL, SP name, endpoint, or selector.</param>
    /// <param name="sortOrder">Display order.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <returns>The newly created <see cref="ReportDataSource"/>.</returns>
    /// <exception cref="ReportingDomainException">
    /// Thrown when the report is Archived or a data source with the same name already exists.
    /// </exception>
    public ReportDataSource AddDataSource(
        string name,
        ReportDataSourceType dataSourceType,
        string connectionStringName,
        string queryText,
        int sortOrder,
        string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        if (_dataSources.Exists(ds => ds.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ReportingDomainException(
                $"A data source named '{name}' already exists on this report definition.");
        }

        ReportDataSource dataSource = ReportDataSource.Create(
            Id, name, dataSourceType, connectionStringName, queryText, sortOrder);

        _dataSources.Add(dataSource);
        Touch(modifiedBy);

        return dataSource;
    }

    /// <summary>
    /// Replaces the query text of an existing data source.
    /// </summary>
    /// <param name="dataSourceId">Id of the data source to update.</param>
    /// <param name="queryText">New query text.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">Thrown when the data source is not found or the report is Archived.</exception>
    public void UpdateDataSourceQuery(Guid dataSourceId, string queryText, string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        ReportDataSource dataSource = FindDataSourceOrThrow(dataSourceId);
        dataSource.UpdateQueryText(queryText);
        Touch(modifiedBy);
    }

    /// <summary>
    /// Updates all mutable fields of an existing data source in a single operation.
    /// </summary>
    /// <param name="dataSourceId">Id of the data source to update.</param>
    /// <param name="name">New logical name (must remain unique within this report, non-empty, max 100 chars).</param>
    /// <param name="dataSourceType">New connection technology type.</param>
    /// <param name="connectionStringName">New named connection string reference.</param>
    /// <param name="queryText">New SQL, SP name, endpoint, or selector.</param>
    /// <param name="sortOrder">New display order.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">
    /// Thrown when the report is Archived, the data source is not found, or a different data source
    /// with the same <paramref name="name"/> already exists on this report.
    /// </exception>
    public void UpdateDataSource(
        Guid dataSourceId,
        string name,
        ReportDataSourceType dataSourceType,
        string connectionStringName,
        string queryText,
        int sortOrder,
        string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        ReportDataSource dataSource = FindDataSourceOrThrow(dataSourceId);

        if (_dataSources.Exists(ds =>
                ds.Id != dataSourceId &&
                ds.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ReportingDomainException(
                $"A data source named '{name}' already exists on this report definition.");
        }

        dataSource.Update(name, dataSourceType, connectionStringName, queryText, sortOrder);
        Touch(modifiedBy);
    }

    /// <summary>
    /// Removes a data source from this report definition.
    /// </summary>
    /// <param name="dataSourceId">Id of the data source to remove.</param>
    /// <param name="modifiedBy">Identity performing the change.</param>
    /// <exception cref="ReportingDomainException">
    /// Thrown when the data source is not found, the report is Archived, or removal would leave
    /// the report with zero data sources while it is Active.
    /// </exception>
    public void RemoveDataSource(Guid dataSourceId, string modifiedBy)
    {
        EnsureNotArchived();
        Guard.NotNullOrWhiteSpace(modifiedBy, nameof(modifiedBy));

        ReportDataSource dataSource = FindDataSourceOrThrow(dataSourceId);

        if (Status == ReportStatus.Active && _dataSources.Count == 1)
        {
            throw new ReportingDomainException(
                "Cannot remove the last data source from an Active report. Deactivate the report first.");
        }

        _dataSources.Remove(dataSource);
        Touch(modifiedBy);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnsureNotArchived()
    {
        if (Status == ReportStatus.Archived)
        {
            throw new ReportingDomainException(
                "Archived report definitions are immutable and cannot be modified.");
        }
    }

    private ReportParameter FindParameterOrThrow(Guid parameterId)
    {
        return _parameters.Find(p => p.Id == parameterId)
            ?? throw new ReportingDomainException($"Parameter with id '{parameterId}' was not found.");
    }

    private ReportDataSource FindDataSourceOrThrow(Guid dataSourceId)
    {
        return _dataSources.Find(ds => ds.Id == dataSourceId)
            ?? throw new ReportingDomainException($"Data source with id '{dataSourceId}' was not found.");
    }

    private void Touch(string modifiedBy)
    {
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = modifiedBy;
    }
}
