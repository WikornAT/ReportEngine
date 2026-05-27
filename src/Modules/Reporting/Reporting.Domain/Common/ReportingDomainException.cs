namespace Reporting.Domain.Common;

/// <summary>
/// Base exception for all domain rule violations in the Reporting module.
/// Throw this (or a derived type) to signal that a domain invariant has been broken.
/// </summary>
public class ReportingDomainException : Exception
{
    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public ReportingDomainException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and an <paramref name="innerException"/> that is the cause of this exception.
    /// </summary>
    public ReportingDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
