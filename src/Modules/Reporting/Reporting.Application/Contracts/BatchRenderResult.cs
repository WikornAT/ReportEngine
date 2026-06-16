using Reporting.Domain.Enums;

namespace Reporting.Application.Contracts;

/// <summary>
/// Outcome of a batch render operation returned by <see cref="IBatchReportOrchestrator"/>.
/// </summary>
/// <param name="Mode">The <see cref="RenderMode"/> that was executed.</param>
/// <param name="Content">Raw binary output (PDF bytes, ZIP bytes, or UTF-8 HTML bytes).</param>
/// <param name="ContentType">MIME type of <paramref name="Content"/>.</param>
/// <param name="FileName">Suggested file name including extension.</param>
/// <param name="BatchExecutionId">Shared identifier for all executions in this batch.</param>
/// <param name="Items">Per-item outcome details.</param>
public sealed record BatchRenderResult(
    RenderMode Mode,
    byte[] Content,
    string ContentType,
    string FileName,
    Guid BatchExecutionId,
    IReadOnlyList<BatchItemResult> Items);

/// <summary>
/// Outcome of rendering a single item within a batch.
/// </summary>
/// <param name="Index">Zero-based index of this item.</param>
/// <param name="Success">Whether this item rendered successfully.</param>
/// <param name="ErrorMessage">Error description when <paramref name="Success"/> is <see langword="false"/>.</param>
/// <param name="DurationMs">Wall-clock duration of this item's render in milliseconds.</param>
/// <param name="OutputFileName">Resolved output file name for this item (e.g., <c>invoice_0.pdf</c>).</param>
public sealed record BatchItemResult(
    int Index,
    bool Success,
    string? ErrorMessage,
    long DurationMs,
    string? OutputFileName);
