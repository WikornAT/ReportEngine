using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportSchedules;

public sealed class ScheduledReport
{
    public Guid Id { get; private set; }
    public Guid ReportDefinitionId { get; private set; }
    public ReportScheduleType ScheduleType { get; private set; }
    public string ParametersJson { get; private set; } = "{}";
    public string RequestedFormatsCsv { get; private set; } = "Pdf";
    public string TriggeredBy { get; private set; } = "system";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    private ScheduledReport() { }
}
