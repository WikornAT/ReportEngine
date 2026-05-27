using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportDefinitions;

/// <summary>
/// Represents a named, typed input parameter declared on a <see cref="ReportDefinition"/>.
/// <para>
/// Parameters are owned child entities of <see cref="ReportDefinition"/> and must not be
/// created independently. All mutations are performed through the aggregate root.
/// </para>
/// <para>
/// <b>Extension point:</b> Add <c>ValidValues</c> (IReadOnlyList&lt;string&gt;) and
/// <c>DependsOnParameterName</c> (string?) here when cascading parameter support is needed.
/// </para>
/// </summary>
public sealed class ReportParameter
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Foreign key to the owning <see cref="ReportDefinition"/>.</summary>
    public Guid ReportDefinitionId { get; private set; }

    // ── Descriptor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Unique name within the report, used as the binding token in the report template
    /// (e.g., <c>@StartDate</c>).  Must be non-empty and unique within the definition.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Human-readable label shown in the report viewer/runner UI.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Optional description or tooltip text for the parameter.</summary>
    public string? Description { get; private set; }

    // ── Type & Constraints ────────────────────────────────────────────────────

    /// <summary>The data type that the parameter value must conform to.</summary>
    public ReportParameterType ParameterType { get; private set; }

    /// <summary>
    /// Whether the user must supply a value before the report can be executed.
    /// When <see langword="false"/>, <see cref="DefaultValue"/> is used if no value is provided.
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Optional default value serialized as a string.
    /// The reporting engine deserializes this according to <see cref="ParameterType"/>.
    /// </summary>
    public string? DefaultValue { get; private set; }

    /// <summary>
    /// Display order in the parameter input form.  Lower values appear first.
    /// </summary>
    public int SortOrder { get; private set; }

    // ── Visibility ────────────────────────────────────────────────────────────

    /// <summary>
    /// When <see langword="false"/> the parameter is resolved internally and not surfaced in the UI.
    /// Useful for system-injected values such as <c>CurrentUserId</c>.
    /// </summary>
    public bool IsVisible { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Private parameterless constructor required by EF Core.
    /// Do not use directly; use <see cref="Create"/> instead.
    /// </summary>
    private ReportParameter() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new, valid <see cref="ReportParameter"/> child entity.
    /// Called exclusively by <see cref="ReportDefinition.AddParameter"/>.
    /// </summary>
    /// <param name="reportDefinitionId">Id of the owning aggregate root.</param>
    /// <param name="name">Binding token name (non-empty, max 100 characters).</param>
    /// <param name="displayName">UI label (non-empty, max 200 characters).</param>
    /// <param name="parameterType">Data type for this parameter.</param>
    /// <param name="isRequired">Whether a value must be supplied at execution time.</param>
    /// <param name="defaultValue">Optional default value string.</param>
    /// <param name="sortOrder">Display order in the UI form; must be &gt; 0.</param>
    /// <param name="isVisible">Whether to show the parameter in the UI.</param>
    /// <param name="description">Optional description or tooltip text.</param>
    /// <returns>A new <see cref="ReportParameter"/> instance.</returns>
    internal static ReportParameter Create(
        Guid reportDefinitionId,
        string name,
        string displayName,
        ReportParameterType parameterType,
        bool isRequired,
        string? defaultValue,
        int sortOrder,
        bool isVisible,
        string? description = null)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrWhiteSpace(displayName, nameof(displayName));
        Guard.DefinedEnum(parameterType, nameof(parameterType));

        if (name.Length > 100)
        {
            throw new ReportingDomainException($"'{nameof(name)}' must not exceed 100 characters.");
        }

        if (displayName.Length > 200)
        {
            throw new ReportingDomainException($"'{nameof(displayName)}' must not exceed 200 characters.");
        }

        return new ReportParameter
        {
            Id = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            Name = name,
            DisplayName = displayName,
            Description = description,
            ParameterType = parameterType,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            SortOrder = sortOrder,
            IsVisible = isVisible,
        };
    }

    // ── Domain behaviour ──────────────────────────────────────────────────────

    /// <summary>
    /// Updates the display metadata for this parameter.
    /// </summary>
    /// <param name="displayName">New UI label (non-empty).</param>
    /// <param name="description">New tooltip text (optional).</param>
    /// <param name="sortOrder">New display order.</param>
    internal void UpdateDisplayMetadata(string displayName, string? description, int sortOrder)
    {
        Guard.NotNullOrWhiteSpace(displayName, nameof(displayName));

        DisplayName = displayName;
        Description = description;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Sets or clears the default value for this parameter.
    /// </summary>
    /// <param name="defaultValue">Serialized default value, or <see langword="null"/> to clear it.</param>
    internal void SetDefaultValue(string? defaultValue)
    {
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Controls whether this parameter is visible in the report runner UI.
    /// </summary>
    /// <param name="isVisible"><see langword="true"/> to show; <see langword="false"/> to hide.</param>
    internal void SetVisibility(bool isVisible)
    {
        IsVisible = isVisible;
    }
}
