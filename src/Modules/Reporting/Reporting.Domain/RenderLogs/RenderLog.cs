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

    // ── Per-phase timing ──────────────────────────────────────────────────────

    /// <summary>Resolved parameters JSON captured at render time (for audit/replay).</summary>
    public string? ParametersJson { get; private set; }

    /// <summary>Time spent executing data source queries, in milliseconds.</summary>
    public long? DataSourceExecutionMs { get; private set; }

    /// <summary>Time spent binding the Scriban template, in milliseconds.</summary>
    public long? TemplateBindingMs { get; private set; }

    /// <summary>Time spent in the PDF renderer (Playwright), in milliseconds. Null for HTML previews.</summary>
    public long? RenderMs { get; private set; }

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

    /// <summary>
    /// Records per-phase timing and the resolved parameters used for this render.
    /// Call before <see cref="Succeed"/> or <see cref="Fail"/>.
    /// </summary>
    public void RecordPhaseTimings(
        string? parametersJson,
        long dataSourceExecutionMs,
        long templateBindingMs,
        long renderMs)
    {
        // Normalize: store null rather than an empty/whitespace string to avoid
        // inserting invalid content into the jsonb column.
        ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? null : parametersJson;
        DataSourceExecutionMs = dataSourceExecutionMs;
        TemplateBindingMs = templateBindingMs;
        RenderMs = renderMs;
    }

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
