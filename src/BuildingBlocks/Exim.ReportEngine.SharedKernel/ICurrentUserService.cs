namespace Exim.ReportEngine.SharedKernel;

/// <summary>
/// Provides identity information about the currently authenticated user.
/// Used by command handlers to stamp <c>CreatedBy</c> / <c>ModifiedBy</c> fields
/// on aggregates without coupling to ASP.NET Core's <c>HttpContext</c>.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The unique identifier (username, email, or subject claim) of the current user.
    /// Returns <c>"system"</c> for background/worker-triggered operations.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// The display name of the current user, or <see langword="null"/> when not available.
    /// </summary>
    public string? DisplayName { get; }
}
