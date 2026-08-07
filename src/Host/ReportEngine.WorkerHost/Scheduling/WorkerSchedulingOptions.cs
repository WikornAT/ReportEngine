namespace ReportEngine.WorkerHost.Scheduling;

internal sealed class WorkerSchedulingOptions
{
    public const string SectionName = "Worker:Scheduling";

    public string DailyCron { get; set; } = "0 0 8 * * ?";

    public string TimeZoneId { get; set; } = "SE Asia Standard Time";
}
