using Reporting.Domain.Enums;

namespace ReportEngine.WorkerHost.Scheduling;

internal static class ScheduleCalendarPolicy
{
    public static IReadOnlySet<ReportScheduleType> GetDueScheduleTypes(DateTimeOffset localNow)
    {
        var due = new HashSet<ReportScheduleType>();

        if (localNow.DayOfWeek == DayOfWeek.Sunday)
        {
            due.Add(ReportScheduleType.Weekly);
        }
        else
        {
            due.Add(ReportScheduleType.Daily);
        }

        DateTime date = localNow.Date;
        bool isMonthEnd = date.AddDays(1).Month != date.Month;

        if (isMonthEnd)
        {
            due.Add(ReportScheduleType.Monthly);

            if (date.Month is 3 or 6 or 9 or 12)
            {
                due.Add(ReportScheduleType.Quarterly);
            }

            if (date.Month == 12 && date.Day == 31)
            {
                due.Add(ReportScheduleType.Yearly);
            }
        }

        return due;
    }
}
