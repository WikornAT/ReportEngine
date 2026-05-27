namespace Reporting.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a single <see cref="ReportExecutions.ReportExecution"/> run.
/// State transitions are enforced by the aggregate; see <c>ReportExecution</c> for valid paths.
/// </summary>
public enum ReportExecutionStatus
{
    /// <summary>The execution request has been accepted and is waiting in queue.</summary>
    Queued = 0,

    /// <summary>The reporting engine is actively processing the execution.</summary>
    Running = 1,

    /// <summary>The execution completed successfully and output files are available.</summary>
    Completed = 2,

    /// <summary>The execution failed due to a runtime error; see <c>ErrorMessage</c> for details.</summary>
    Failed = 3,

    /// <summary>The execution was cancelled before or during processing.</summary>
    Cancelled = 4,

    /// <summary>The execution timed out and was terminated by the engine.</summary>
    TimedOut = 5,
}
