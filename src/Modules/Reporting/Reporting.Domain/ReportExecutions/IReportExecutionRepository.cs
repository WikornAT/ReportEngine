using Exim.ReportEngine.Abstractions.Repositories;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportExecutions;

/// <summary>
/// Domain-owned repository contract for <see cref="ReportExecution"/>.
/// Implementations live in <c>Reporting.Infrastructure</c>.
/// </summary>
public interface IReportExecutionRepository : IRepository<ReportExecution, Guid>
{
    /// <summary>
    /// Returns all executions for a given report definition, most recent first.
    /// </summary>
    /// <param name="reportDefinitionId">The report definition to filter by.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<IReadOnlyList<ReportExecution>> GetByReportDefinitionIdAsync(
        Guid reportDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all executions triggered by the given <paramref name="triggeredBy"/> identity,
    /// most recent first.
    /// </summary>
    /// <param name="triggeredBy">The identity (username or system account) to filter by.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<IReadOnlyList<ReportExecution>> GetByTriggeredByAsync(
        string triggeredBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all executions currently in <see cref="ReportExecutionStatus.Queued"/> status,
    /// ordered by <c>CreatedAt</c> ascending (FIFO).
    /// Used by the worker service to dequeue pending executions.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<IReadOnlyList<ReportExecution>> GetQueuedExecutionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all executions that have been in <see cref="ReportExecutionStatus.Running"/> status
    /// for longer than <paramref name="runningForMoreThan"/>.
    /// Used by the worker service to detect and time-out stale executions.
    /// </summary>
    /// <param name="runningForMoreThan">The duration threshold.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<IReadOnlyList<ReportExecution>> GetStaleRunningExecutionsAsync(
        TimeSpan runningForMoreThan,
        CancellationToken cancellationToken = default);
}
