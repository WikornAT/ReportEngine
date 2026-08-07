namespace Reporting.Domain.Enums;

/// <summary>
/// Frequency bucket used by worker-side scheduling policy.
/// </summary>
public enum ReportScheduleType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}
