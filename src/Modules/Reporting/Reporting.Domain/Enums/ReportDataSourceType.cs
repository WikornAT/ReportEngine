namespace Reporting.Domain.Enums;

/// <summary>
/// Describes the origin technology or protocol of a report data source.
/// Used to determine how the reporting engine connects to and queries data.
/// </summary>
public enum ReportDataSourceType
{
    /// <summary>Direct SQL query executed against a relational database connection.</summary>
    SqlQuery = 0,

    /// <summary>Stored procedure call on a relational database server.</summary>
    StoredProcedure = 1,

    /// <summary>REST or SOAP web service endpoint.</summary>
    WebService = 2,

    /// <summary>Static or dynamically generated JSON payload.</summary>
    Json = 3,

    /// <summary>Static or dynamically generated XML document.</summary>
    Xml = 4,

    /// <summary>OData v4 feed endpoint.</summary>
    OData = 5,

    /// <summary>In-process .NET object model provided by the host application.</summary>
    InMemory = 6,

    /// <summary>Custom/pluggable data source registered via the extensibility API.</summary>
    Custom = 99,
}
