namespace Reporting.Domain.Enums;

/// <summary>
/// Describes the data type of a <see cref="ReportDefinitions.ReportParameter"/>,
/// which drives UI rendering, input validation, and SQL binding.
/// </summary>
public enum ReportParameterType
{
    /// <summary>Free-form text value.</summary>
    Text = 0,

    /// <summary>32-bit signed integer value.</summary>
    WholeNumber = 1,

    /// <summary>High-precision decimal value (suitable for monetary amounts).</summary>
    Numeric = 2,

    /// <summary>Boolean true/false toggle.</summary>
    Boolean = 3,

    /// <summary>Calendar date (no time component).</summary>
    Date = 4,

    /// <summary>Date and time with optional time-zone offset.</summary>
    DateTime = 5,

    /// <summary>Globally unique identifier.</summary>
    UniqueIdentifier = 6,

    /// <summary>Allows the user to select one or more values from a predefined list.</summary>
    MultiValue = 7,

    /// <summary>Cascading drop-down whose available values depend on a parent parameter.</summary>
    CascadingValue = 8,
}
