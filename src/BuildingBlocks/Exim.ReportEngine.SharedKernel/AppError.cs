namespace Exim.ReportEngine.SharedKernel;

/// <summary>
/// Represents a typed, immutable application-level error.
/// Carried inside <see cref="Result{T}"/> to describe why an operation failed
/// without throwing an exception.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g., <c>Reporting.NotFound</c>).</param>
/// <param name="Message">Human-readable description of the failure.</param>
public sealed record AppError(string Code, string Message)
{
    // ── Well-known error factories ────────────────────────────────────────────

    /// <summary>Creates a not-found error for a given resource type and identifier.</summary>
    public static AppError NotFound(string resource, object id) =>
        new($"{resource}.NotFound", $"{resource} with id '{id}' was not found.");

    /// <summary>Creates a validation error with the given <paramref name="message"/>.</summary>
    public static AppError Validation(string message) =>
        new("Validation", message);

    /// <summary>Creates a conflict error (e.g., duplicate name).</summary>
    public static AppError Conflict(string message) =>
        new("Conflict", message);

    /// <summary>Creates a domain rule-violation error.</summary>
    public static AppError DomainViolation(string message) =>
        new("DomainViolation", message);

    /// <summary>Creates a generic unexpected-error descriptor.</summary>
    public static AppError Unexpected(string message) =>
        new("Unexpected", message);

    /// <summary>The singleton empty / no-error sentinel used on successful results.</summary>
    public static readonly AppError None = new(string.Empty, string.Empty);
}
