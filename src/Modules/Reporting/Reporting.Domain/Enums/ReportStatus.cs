namespace Reporting.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="ReportDefinitions.ReportDefinition"/>.
/// Controls whether the report is visible and available for execution.
/// </summary>
public enum ReportStatus
{
    /// <summary>The report is in draft state and not yet available for scheduling or on-demand execution.</summary>
    Draft = 0,

    /// <summary>The report is published and available for execution by authorized users.</summary>
    Active = 1,

    /// <summary>The report has been retired and is no longer available for new executions, but historical data is preserved.</summary>
    Inactive = 2,

    /// <summary>The report definition is archived for audit/compliance purposes and is read-only.</summary>
    Archived = 3,
}
