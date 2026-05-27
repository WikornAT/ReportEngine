using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportExecutions;

/// <summary>
/// Aggregate root representing a single run of a report.
/// <para>
/// A <see cref="ReportExecution"/> is the runtime counterpart of
/// <see cref="ReportDefinitions.ReportDefinition"/>. It captures <em>when</em> a report was run,
/// <em>who</em> ran it, <em>what parameters</em> were supplied, the execution outcome, and
/// any output files that were produced.
/// </para>
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
/// <b>Aggregate boundary:</b>
/// <list type="bullet">
///   <item><see cref="ReportOutputFile"/> — owned child, appended via <see cref="AddOutputFile"/>
///   after a successful render.</item>
/// </list>
/// </para>
/// <para>
/// <b>Extension points:</b> Add <c>ScheduledExecutionId</c> (Guid?) to link back to a
/// Scheduling module trigger; add <c>PrintJobId</c> (Guid?) for Printing integration;
/// add <c>NotificationsSent</c> (bool) for delivery tracking.
/// </para>
/// </summary>
public sealed class ReportExecution : IAuditableEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    // ── Report reference ──────────────────────────────────────────────────────

    /// <summary>
    /// Cross-aggregate reference to the <see cref="ReportDefinitions.ReportDefinition"/>
    /// that was executed.  The application layer resolves the full definition via
    /// <c>IReportDefinitionRepository</c> when needed.
    /// </summary>
    public Guid ReportDefinitionId { get; private set; }

    /// <summary>
    /// Snapshot of the report name at execution time.
    /// Preserved so that renaming or archiving the definition does not corrupt history.
    /// </summary>
    public string ReportName { get; private set; } = string.Empty;

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>
    /// JSON-serialized snapshot of the parameter values supplied by the caller.
    /// The schema matches the parameter declarations on the associated
    /// <see cref="ReportDefinitions.ReportDefinition"/> at execution time.
    /// Stored as a raw JSON string to keep the domain free of serializer dependencies.
    /// </summary>
    public string ParametersJson { get; private set; } = "{}";

    // ── Requested formats ─────────────────────────────────────────────────────

    /// <summary>
    /// One or more output formats requested by the caller.
    /// The engine produces one <see cref="ReportOutputFile"/> per requested format.
    /// </summary>
    public IReadOnlyList<ReportOutputFormat> RequestedFormats => _requestedFormats.AsReadOnly();

    private readonly List<ReportOutputFormat> _requestedFormats = [];

    // ── Status & timing ───────────────────────────────────────────────────────

    /// <summary>Current lifecycle status of this execution run.</summary>
    public ReportExecutionStatus Status { get; private set; }

    /// <summary>UTC timestamp when the execution transitioned to <see cref="ReportExecutionStatus.Running"/>.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the execution reached a terminal state
    /// (Completed, Failed, Cancelled, or TimedOut).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Duration of the execution in milliseconds.
    /// Populated when the execution reaches a terminal state.
    /// </summary>
    public long? DurationMs { get; private set; }

    // ── Outcome ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Human-readable error message when <see cref="Status"/> is
    /// <see cref="ReportExecutionStatus.Failed"/> or <see cref="ReportExecutionStatus.TimedOut"/>.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Number of data rows returned by the primary data source query.</summary>
    public int? RowCount { get; private set; }

    // ── Trigger ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Identity (user or system account) that initiated this execution.
    /// </summary>
    public string TriggeredBy { get; private set; } = string.Empty;

    /// <summary>
    /// Optional correlation token supplied by the caller (e.g., a client request id).
    /// Useful for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; private set; }

    // ── Output files ─────────────────────────────────────────────────────────

    private readonly List<ReportOutputFile> _outputFiles = [];

    /// <summary>Files rendered by the engine for this execution run.</summary>
    public IReadOnlyList<ReportOutputFile> OutputFiles => _outputFiles.AsReadOnly();

    // ── Audit ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <inheritdoc/>
    public string? ModifiedBy { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Private parameterless constructor required by EF Core.
    /// Do not use directly; use <see cref="Queue"/> instead.
    /// </summary>
    private ReportExecution() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="ReportExecution"/> in <see cref="ReportExecutionStatus.Queued"/> state.
    /// </summary>
    /// <param name="reportDefinitionId">Reference to the report definition being executed.</param>
    /// <param name="reportName">Snapshot of the report name at queue time (non-empty).</param>
    /// <param name="parametersJson">JSON-serialized parameter values (non-empty; use <c>{}</c> for no params).</param>
    /// <param name="requestedFormats">At least one output format must be requested.</param>
    /// <param name="triggeredBy">Identity initiating the execution (non-empty).</param>
    /// <param name="correlationId">Optional distributed-trace correlation token.</param>
    /// <returns>A new <see cref="ReportExecution"/> in Queued status.</returns>
    /// <exception cref="ReportingDomainException">
    /// Thrown when <paramref name="requestedFormats"/> is empty or contains undefined values.
    /// </exception>
    public static ReportExecution Queue(
        Guid reportDefinitionId,
        string reportName,
        string parametersJson,
        IEnumerable<ReportOutputFormat> requestedFormats,
        string triggeredBy,
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

        DateTimeOffset now = DateTimeOffset.UtcNow;

        ReportExecution execution = new()
        {
            Id = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            ReportName = reportName,
            ParametersJson = parametersJson,
            Status = ReportExecutionStatus.Queued,
            TriggeredBy = triggeredBy,
            CorrelationId = correlationId,
            CreatedAt = now,
            CreatedBy = triggeredBy,
        };

        execution._requestedFormats.AddRange(formats);

        return execution;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Marks the execution as actively running.
    /// Valid only from <see cref="ReportExecutionStatus.Queued"/>.
    /// </summary>
    /// <exception cref="ReportingDomainException">Thrown on an invalid transition.</exception>
    public void Start()
    {
        EnsureTransition(ReportExecutionStatus.Queued, ReportExecutionStatus.Running);

        Status = ReportExecutionStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
        Touch(TriggeredBy);
    }

    /// <summary>
    /// Marks the execution as successfully completed.
    /// Valid only from <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="rowCount">Number of data rows returned by the primary data source.</param>
    /// <exception cref="ReportingDomainException">Thrown on an invalid transition.</exception>
    public void Complete(int? rowCount = null)
    {
        EnsureTransition(ReportExecutionStatus.Running, ReportExecutionStatus.Completed);

        Status = ReportExecutionStatus.Completed;
        RowCount = rowCount;
        MarkTerminal();
        Touch(TriggeredBy);
    }

    /// <summary>
    /// Marks the execution as failed due to a runtime error.
    /// Valid only from <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="errorMessage">Human-readable error detail (non-empty).</param>
    /// <exception cref="ReportingDomainException">Thrown on an invalid transition.</exception>
    public void Fail(string errorMessage)
    {
        Guard.NotNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        EnsureTransition(ReportExecutionStatus.Running, ReportExecutionStatus.Failed);

        Status = ReportExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        MarkTerminal();
        Touch(TriggeredBy);
    }

    /// <summary>
    /// Marks the execution as timed out.
    /// Valid only from <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="errorMessage">Optional context describing the timeout (non-empty).</param>
    /// <exception cref="ReportingDomainException">Thrown on an invalid transition.</exception>
    public void TimeOut(string errorMessage)
    {
        Guard.NotNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        EnsureTransition(ReportExecutionStatus.Running, ReportExecutionStatus.TimedOut);

        Status = ReportExecutionStatus.TimedOut;
        ErrorMessage = errorMessage;
        MarkTerminal();
        Touch(TriggeredBy);
    }

    /// <summary>
    /// Cancels the execution.
    /// Valid from <see cref="ReportExecutionStatus.Queued"/> or <see cref="ReportExecutionStatus.Running"/>.
    /// </summary>
    /// <param name="cancelledBy">Identity requesting the cancellation (non-empty).</param>
    /// <exception cref="ReportingDomainException">Thrown when the execution is already in a terminal state.</exception>
    public void Cancel(string cancelledBy)
    {
        Guard.NotNullOrWhiteSpace(cancelledBy, nameof(cancelledBy));

        if (Status is not (ReportExecutionStatus.Queued or ReportExecutionStatus.Running))
        {
            throw new ReportingDomainException(
                $"Cannot cancel a report execution in '{Status}' status. " +
                "Only Queued or Running executions can be cancelled.");
        }

        Status = ReportExecutionStatus.Cancelled;
        MarkTerminal();
        Touch(cancelledBy);
    }

    // ── Output file management ────────────────────────────────────────────────

    /// <summary>
    /// Records a rendered output file on this execution.
    /// Valid only when <see cref="Status"/> is <see cref="ReportExecutionStatus.Running"/>
    /// or <see cref="ReportExecutionStatus.Completed"/>.
    /// </summary>
    /// <param name="outputFormat">Render format of the file.</param>
    /// <param name="fileName">Original file name with extension.</param>
    /// <param name="storagePath">Infrastructure storage key or path.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="fileSizeBytes">File size in bytes.</param>
    /// <returns>The newly created <see cref="ReportOutputFile"/>.</returns>
    /// <exception cref="ReportingDomainException">Thrown when called in an invalid status.</exception>
    public ReportOutputFile AddOutputFile(
        ReportOutputFormat outputFormat,
        string fileName,
        string storagePath,
        string contentType,
        long fileSizeBytes)
    {
        if (Status is not (ReportExecutionStatus.Running or ReportExecutionStatus.Completed))
        {
            throw new ReportingDomainException(
                $"Output files can only be added to a Running or Completed execution. Current status: '{Status}'.");
        }

        ReportOutputFile file = ReportOutputFile.Create(
            Id, outputFormat, fileName, storagePath, contentType, fileSizeBytes);

        _outputFiles.Add(file);
        Touch(TriggeredBy);

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

    private void MarkTerminal()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CompletedAt = now;
        DurationMs = StartedAt.HasValue
            ? (long)(now - StartedAt.Value).TotalMilliseconds
            : null;
    }

    private void Touch(string actor)
    {
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = actor;
    }
}
