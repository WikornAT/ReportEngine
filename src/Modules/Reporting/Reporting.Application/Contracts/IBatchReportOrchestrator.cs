using Reporting.Domain.Enums;

namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for orchestrating batch report rendering across multiple parameter sets.
/// <para>
/// The implementation lives in <c>Reporting.Infrastructure</c> and delegates
/// to <see cref="IReportRenderer"/> and <see cref="IReportQueryExecutor"/> per item.
/// It also persists a parent <see cref="Domain.ReportExecutions.ReportExecution"/> and one
/// child execution per parameter set.
/// </para>
/// </summary>
public interface IBatchReportOrchestrator
{
    /// <summary>
    /// Renders a report for each item in <paramref name="parametersJsonItems"/> according
    /// to <paramref name="mode"/> and returns the combined output.
    /// </summary>
    /// <param name="reportDefinitionId">The report definition to render.</param>
    /// <param name="reportName">Snapshot of the report name for execution logs.</param>
    /// <param name="mode">How to produce the output from multiple parameter sets.</param>
    /// <param name="parametersJsonItems">
    /// Ordered list of per-item JSON parameter objects
    /// (e.g., <c>{"invoiceNo":"INV-001"}</c>). Must contain at least one element.
    /// </param>
    /// <param name="batchExecutionId">Shared identifier emitted on all execution records.</param>
    /// <param name="outputFileNamePattern">
    /// Optional naming pattern for individual output files.
    /// Supports <c>{reportName}</c>, <c>{index}</c>, <c>{batchExecutionId}</c>.
    /// Defaults to <c>{reportName}_{index}</c> when <see langword="null"/>.
    /// </param>
    /// <param name="continueOnError">
    /// When <see langword="true"/> and <paramref name="mode"/> is
    /// <see cref="RenderMode.ZipPdf"/> or <see cref="RenderMode.PreviewHtml"/>,
    /// failed items are skipped rather than aborting the whole batch.
    /// </param>
    /// <param name="triggeredBy">Identity of the caller for audit logs.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>
    /// A <see cref="BatchRenderResult"/> with the combined output and per-item outcomes.
    /// </returns>
    Task<BatchRenderResult> RenderBatchAsync(
        Guid reportDefinitionId,
        string reportName,
        RenderMode mode,
        IReadOnlyList<string> parametersJsonItems,
        Guid batchExecutionId,
        string? outputFileNamePattern,
        bool continueOnError,
        string triggeredBy,
        CancellationToken cancellationToken = default);
}
