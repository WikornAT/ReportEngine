namespace ReportEngine.SharedKernel;

/// <summary>
/// Provides the current UTC date and time.
/// Abstracted so that handlers and validators can be tested with deterministic clocks.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Returns the current date and time expressed as UTC.</summary>
    public DateTimeOffset UtcNow { get; }
}
