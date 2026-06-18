using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportExecutions;

/// <summary>
/// Aggregate root representing a single run of a report.
/// <para>
/// <b>State machine:</b>
/// <code>
/// Queued → Running → Completed
///                  → Failed
///                  → TimedOut
///        → Cancelled  (from Queued or Running)
/// </code>
/// Transitions are enforced by this aggregate; invalid transitions throw
/// <see cref="ReportingDomainException"/>.
/// </para>
/// <para>
/// <b>Clock discipline:</b> this aggregate is clock-free. Every method that records a timestamp
/// requires the caller to supply <c>DateTimeOffset now</c> — obtained from
/// <c>IDateTimeProvider.UtcNow</c> in the Application or Infrastructure layer.
/// </para>
/// </summary>
public sealed class ReportExecution : IAuditableEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public Guid Id { get; private set; }

    // ── Report reference ──────────────────────────────────────────────────────

    public Guid ReportDefinitionId { get; private set; }

    /// <summary>Snapshot of the report name at execution time.</summary>
    public string ReportName { get; private set; } = string.Empty;

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>JSON-serialized snapshot of the parameter values supplied by the caller.</summary>
    public string ParametersJson { get; private set; } = "{}";

    // ── Requested formats ─────────────────────────────────────────────────────

    public IReadOnlyList<ReportOutputFormat> RequestedFormats => _requestedFormats.AsReadOnly();
    private readonly List<ReportOutputFormat> _requestedFormats = [];

    // ── Status & timing ───────────────────────────────────────────────────────

    public ReportExecutionStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long? DurationMs { get; private set; }

    // ── Outcome ───────────────────────────────────────────────────────────────

    public string? ErrorMessage { get; private set; }
    public int? RowCount { get; private set; }

    // ── Trigger ───────────────────────────────────────────────────────────────

    public string TriggeredBy { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }

    // ── Batch tracking ────────────────────────────────────────────────────────

    /// <summary>Shared id for all executions in the same batch. <see langword="null"/> for non-batch executions.</summary>
    public Guid? BatchExecutionId { get; private set; }

    /// <summary>Id of the parent execution record. <see langword="null"/> for the parent itself.</summary>
    public Guid? ParentExecutionId { get; private set; }

    /// <summary>Zero-based item index within the batch. <see langword="null"/> for the parent.</summary>
    public int? BatchItemIndex { get; private set; }

    /// <summary>Render mode used for this execution. <see langword="null"/> for non-batch executions.</summary>
    public RenderMode? RenderMode { get; private set; }

    /// <summary>Resolved output file name for this item.</summary>
    public string? OutputFileName { get; private set; }

    // ── Output files ─────────────────────────────────────────────────────────

    private readonly List<ReportOutputFile> _outputFiles = [];
    public IReadOnlyList<ReportOutputFile> OutputFiles => _outputFiles.AsReadOnly();

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    private ReportExecution() { }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="ReportExecution"/> in <see cref="ReportExecutionStatus.Queued"/> state.
    /// </summary>
    /// <param name="reportDefinitionId">Reference to the report definition being executed.</param>
    /// <param name="reportName">Snapshot of the report name at queue time (non-empty).</param>
    /// <param name="parametersJson">JSON-serialized parameter values (non-empty; use <c>{}</c> for no params).</param>
    /// <param name="requestedFormats">At least one output format must be requested; all values must be defined enum members.</param>
    /// <param name="triggeredBy">Identity initiating the execution (non-empty).</param>
    /// <param name="now">Current UTC timestamp supplied by the caller — not read from the system clock.</param>
    /// <param name="correlationId">Optional distributed-trace correlation token.</param>
    /// <returns>A new <see cref="ReportExecution"/> in Queued status.</returns>
    /// <exception cref="ReportingDomainException">
    /// Thrown when <paramref name="requestedFormats"/> is empty or contains undefined enum values.
    /// </exception>
    public static ReportExecution Queue(
        Guid reportDefinitionId,
        string reportName,
        string parametersJson,
        IEnumerable<ReportOutputFormat> requestedFormats,
        string triggeredBy,
        DateTimeOffset now,
        string? correlationId = null)
    {
        Guard.NotNullOrWhiteSpace(reportName, nameof(reportName));
        Guard.NotNullOrWhiteSpace(parametersJson, nameof(parametersJson));
        Guard.NotNullOrWhiteSpace(triggeredBy, nameof(triggeredBy));

        List<ReportOutputFormat> formats = [.. requestedFormats];

        if (formats.Count == 0)
        {
            throw new ReportingDomainException("At least one output format must be requested.");
        }

        foreach (ReportOutputFormat format in formats)
        {
            Guard.DefinedEnum(format, nameof(requestedFormats));
        }

        ReportExecution execution = new()
        {
            Id                 = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            ReportName         = reportName,
            ParametersJson     = parametersJson,
            Status             = ReportExecutionStatus.Queued,
            TriggeredBy        = triggeredBy,
            CorrelationId      = correlationId,
            CreatedAt          = now,
            CreatedBy          = triggeredBy,
        };

        execution._requestedFormats.AddRange(formats);
        return execution;
    }

    /// <summary>
    /// Creates a new <see cref="ReportExecution"/> in <see cref="ReportExecutionStatus.Queued"/> state
    /// as part of a batch render request.
    /// </summary>
    /// <param name="reportDefinitionId">Reference to the report definition being executed.</param>
    /// <param name="reportName">Snapshot of the report name at queue time.</param>
    /// <param name="parametersJson">JSON-serialized parameter values for this batch item.</param>
    /// <param name="requestedFormats">At least one output format must be requested; all values must be defined enum members.</param>
    /// <param name="triggeredBy">Identity initiating the execution.</param>
    /// <param name="now">Current UTC timestamp supplied by the caller — not read from the system clock.</param>
    /// <param name="renderMode">Render mode of the batch; must be a defined <see cref="Enums.RenderMode"/> value.</param>
    /// <param name="batchExecutionId">Shared id for all executions in the batch.</param>
    /// <param name="parentExecutionId">Id of the parent execution; <see langword="null"/> when this is the parent.</param>
    /// <param name="batchItemIndex">Zero-based index of this item; <see langword="null"/> for the parent execution.</param>
    /// <param name="outputFileName">Resolved output file name for this item.</param>
    /// <exception cref="ReportingDomainException">
    /// Thrown when <paramref name="renderMode"/> or any value in <paramref name="requestedFormats"/>
    /// is not a defined enum member, or when <paramref name="requestedFormats"/> is empty.
    /// </exception>
    public static ReportExecution QueueBatch(
        Guid reportDefinitionId,
        string reportName,
        string parametersJson,
        IEnumerable<ReportOutputFormat> requestedFormats,
        string triggeredBy,
        DateTimeOffset now,
        RenderMode renderMode,
        Guid batchExecutionId,
        Guid? parentExecutionId = null,
        int? batchItemIndex = null,
        string? outputFileName = null)
    {
        Guard.NotNullOrWhiteSpace(reportName, nameof(reportName));
        Guard.NotNullOrWhiteSpace(parametersJson, nameof(parametersJson));
        Guard.NotNullOrWhiteSpace(triggeredBy, nameof(triggeredBy));

        List<ReportOutputFormat> formats = [.. requestedFormats];

        if (formats.Count == 0)
        {
            throw new ReportingDomainException("At least one output format must be requested.");
        }

        Guard.DefinedEnum(renderMode, nameof(renderMode));

        foreach (ReportOutputFormat format in formats)
        {
            Guard.DefinedEnum(format, nameof(requestedFormats));
        }

        ReportExecution execution = new()
        {
            Id                 = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            ReportName         = reportName,
            ParametersJson     = parametersJson,
            Status             = ReportExecutionStatus.Queued,
            TriggeredBy        = triggeredBy,
            RenderMode         = renderMode,
            BatchExecutionId   = batchExecutionId,
            ParentExecutionId  = parentExecutionId,
            BatchItemIndex     = batchItemIndex,
            OutputFileName     = outputFileName,
            CreatedAt          = now,
            CreatedBy          = triggeredBy,
        };

        execution._requestedFormats.AddRange(formats);
        return execution;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Marks the execution as actively running.
    /// Valid only from <see cref="ReportExecutionStatus.Queued"/>.
    /// </summary>
    /// <param name="now">Current UTC timestamp supplied by the caller.</param>
    public void Start(DateTimeOffset now)
    {
        EnsureTransition(ReportExecutionStatus.Queued, ReportExecutionStatus.Running);
        Status    = ReportExecutionStatus.Running;
        StartedAt = now;
        Touch(TriggeredBy, now);
    }

    /// <summary>
    /// Marks the execution as successfully completed.
    /// Valid only from <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="now">Current UTC timestamp supplied by the caller.</param>
    /// <param name="rowCount">Number of data rows returned by the primary data source.</param>
    public void Complete(DateTimeOffset now, int? rowCount = null)
    {
        EnsureTransition(ReportExecutionStatus.Running, ReportExecutionStatus.Completed);
        Status   = ReportExecutionStatus.Completed;
        RowCount = rowCount;
        MarkTerminal(now);
        Touch(TriggeredBy, now);
    }

    /// <summary>
    /// Marks the execution as failed due to a runtime error.
    /// Valid only from <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="errorMessage">Human-readable error detail (non-empty).</param>
    /// <param name="now">Current UTC timestamp supplied by the caller.</param>
    public void Fail(string errorMessage, DateTimeOffset now)
    {
        Guard.NotNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        EnsureTransition(ReportExecutionStatus.Running, ReportExecutionStatus.Failed);
        Status       = ReportExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        MarkTerminal(now);
        Touch(TriggeredBy, now);
    }

    /// <summary>
    /// Marks the execution as timed out.
    /// Valid only from <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="errorMessage">Context describing the timeout (non-empty).</param>
    /// <param name="now">Current UTC timestamp supplied by the caller.</param>
    public void TimeOut(string errorMessage, DateTimeOffset now)
    {
        Guard.NotNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        EnsureTransition(ReportExecutionStatus.Running, ReportExecutionStatus.TimedOut);
        Status       = ReportExecutionStatus.TimedOut;
        ErrorMessage = errorMessage;
        MarkTerminal(now);
        Touch(TriggeredBy, now);
    }

    /// <summary>
    /// Cancels the execution.
    /// Valid from <see cref="ReportExecutionStatus.Queued"/> or <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="cancelledBy">Identity requesting the cancellation (non-empty).</param>
    /// <param name="now">Current UTC timestamp supplied by the caller.</param>
    public void Cancel(string cancelledBy, DateTimeOffset now)
    {
        Guard.NotNullOrWhiteSpace(cancelledBy, nameof(cancelledBy));

        if (Status is not (ReportExecutionStatus.Queued or ReportExecutionStatus.Running))
        {
            throw new ReportingDomainException(
                $"Cannot cancel a report execution in '{Status}' status. " +
                "Only Queued or Running executions can be cancelled.");
        }

        Status = ReportExecutionStatus.Cancelled;
        MarkTerminal(now);
        Touch(cancelledBy, now);
    }

    // ── Output file management ────────────────────────────────────────────────

    /// <summary>
    /// Records a rendered output file on this execution.
    /// Valid only when <see cref="Status"/> is Running or Completed.
    /// </summary>
    /// <param name="outputFormat">Render format of the file.</param>
    /// <param name="fileName">Original file name with extension.</param>
    /// <param name="storagePath">Infrastructure storage key or path.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="fileSizeBytes">File size in bytes.</param>
    /// <param name="now">Current UTC timestamp supplied by the caller.</param>
    /// <returns>The newly created <see cref="ReportOutputFile"/>.</returns>
    /// <exception cref="ReportingDomainException">Thrown when called in an invalid status.</exception>
    public ReportOutputFile AddOutputFile(
        ReportOutputFormat outputFormat,
        string fileName,
        string storagePath,
        string contentType,
        long fileSizeBytes,
        DateTimeOffset now)
    {
        if (Status is not (ReportExecutionStatus.Running or ReportExecutionStatus.Completed))
        {
            throw new ReportingDomainException(
                $"Output files can only be added to a Running or Completed execution. Current status: '{Status}'.");
        }

        ReportOutputFile file = ReportOutputFile.Create(
            Id, outputFormat, fileName, storagePath, contentType, fileSizeBytes, now);

        _outputFiles.Add(file);
        Touch(TriggeredBy, now);

        return file;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnsureTransition(ReportExecutionStatus from, ReportExecutionStatus to)
    {
        if (Status != from)
        {
            throw new ReportingDomainException(
                $"Invalid status transition: cannot move from '{Status}' to '{to}'. Expected current status: '{from}'.");
        }
    }

    private void MarkTerminal(DateTimeOffset now)
    {
        CompletedAt = now;
        DurationMs  = StartedAt.HasValue
            ? (long)(now - StartedAt.Value).TotalMilliseconds
            : null;
    }

    private void Touch(string actor, DateTimeOffset now)
    {
        ModifiedAt = now;
        ModifiedBy = actor;
    }
}
