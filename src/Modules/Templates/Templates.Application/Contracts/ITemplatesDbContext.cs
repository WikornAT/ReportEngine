namespace Templates.Application.Contracts;

/// <summary>
/// Unit-of-work abstraction over the Templates module's EF Core DbContext.
/// </summary>
public interface ITemplatesDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
