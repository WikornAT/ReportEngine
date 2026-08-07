namespace Reporting.Application.Contracts;

/// <summary>
/// Reads active scheduled report definitions from persistent storage.
/// </summary>
public interface IReportScheduleProvider
{
    public Task<IReadOnlyList<ScheduledReportDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default);
}
