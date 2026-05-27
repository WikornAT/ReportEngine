namespace Reporting.Domain.Common;

/// <summary>
/// Marks a domain entity as auditable, carrying standard created/modified metadata.
/// Implementations populate these fields at the persistence layer (e.g., EF Core interceptors).
/// </summary>
public interface IAuditableEntity
{
    /// <summary>UTC timestamp when the entity was first persisted.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Identity (username or system account) that created the entity.</summary>
    public string CreatedBy { get; }

    /// <summary>UTC timestamp of the last modification, or <see langword="null"/> if never modified.</summary>
    public DateTimeOffset? ModifiedAt { get; }

    /// <summary>Identity that last modified the entity, or <see langword="null"/> if never modified.</summary>
    public string? ModifiedBy { get; }
}
