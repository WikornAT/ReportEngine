using Reporting.Domain.Enums;

namespace Reporting.Domain.RenderLogs;

/// <summary>
/// Lightweight execution log for an inline (non-persisted) report render.
/// <para>
/// Created when a caller uses the direct preview or render-pdf endpoints
/// (<c>GET /api/reporting/report-definitions/{id}/preview</c> and
/// <c>POST /api/v1/reports/{id}/render-pdf</c>) rather than the full
/// <c>POST /api/reporting/executions</c> pipeline.
/// </para>
/// </summary>
public sealed class RenderLog
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public Guid Id { get; private set; }

    // ── References ────────────────────────────────────────────────────────────

    /// <summary>Report definition that was rendered.</summary>
    public Guid ReportDefinitionId { get; private set; }

    /// <summary>Template that was used (captured at render time).</summary>
    public Guid? TemplateId { get; private set; }

    // ── Render details ────────────────────────────────────────────────────────

    /// <summary>Output format that was rendered.</summary>
    public ReportOutputFormat Format { get; private set; }

    /// <summary>Terminal status of this render attempt.</summary>
    public RenderLogStatus Status { get; private set; }

    // ── Timing ────────────────────────────────────────────────────────────────

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Duration in milliseconds; null when not yet completed.</summary>
    public long? DurationMs { get; private set; }

    // ── Outcome ───────────────────────────────────────────────────────────────

    /// <summary>Error detail when <see cref="Status"/> is <see cref="RenderLogStatus.Failed"/>.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Size of the rendered output in bytes; null on failure.</summary>
    public int? OutputSizeBytes { get; private set; }

    /// <summary>Identity that triggered the render (user or system).</summary>
    public string TriggeredBy { get; private set; } = string.Empty;

    // ── ORM constructor ───────────────────────────────────────────────────────

    private RenderLog() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static RenderLog Start(
        Guid reportDefinitionId,
        Guid? templateId,
        ReportOutputFormat format,
        string triggeredBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeredBy);

        return new RenderLog
        {
            Id = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            TemplateId = templateId,
            Format = format,
            Status = RenderLogStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            TriggeredBy = triggeredBy,
        };
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    public void Succeed(int outputSizeBytes)
    {
        Status = RenderLogStatus.Completed;
        OutputSizeBytes = outputSizeBytes;
        Finish();
    }

    public void Fail(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        Status = RenderLogStatus.Failed;
        ErrorMessage = errorMessage;
        Finish();
    }

    private void Finish()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }
}
