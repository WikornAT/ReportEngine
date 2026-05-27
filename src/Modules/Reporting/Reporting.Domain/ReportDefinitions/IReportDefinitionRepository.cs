using Exim.ReportEngine.Abstractions.Repositories;

namespace Reporting.Domain.ReportDefinitions;

/// <summary>
/// Domain-owned repository contract for <see cref="ReportDefinition"/>.
/// Implementations live in <c>Reporting.Infrastructure</c>.
/// </summary>
public interface IReportDefinitionRepository : IRepository<ReportDefinition, Guid>
{
    /// <summary>
    /// Returns all report definitions belonging to the specified <paramref name="category"/>,
    /// ordered by name ascending.
    /// </summary>
    /// <param name="category">The category to filter by (case-insensitive).</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<IReadOnlyList<ReportDefinition>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all report definitions whose <c>Name</c> contains <paramref name="searchTerm"/>
    /// (case-insensitive), limited to a reasonable page size by the caller.
    /// </summary>
    /// <param name="searchTerm">Substring to match against the report name.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<IReadOnlyList<ReportDefinition>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when a report definition with the given
    /// <paramref name="name"/> and <paramref name="category"/> already exists.
    /// Used to enforce uniqueness before persisting a new definition.
    /// </summary>
    /// <param name="name">Report name to check.</param>
    /// <param name="category">Category to scope the uniqueness check to.</param>
    /// <param name="excludeId">
    /// Optional id to exclude (used during update to avoid false positives).
    /// </param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    public Task<bool> ExistsByNameInCategoryAsync(
        string name,
        string category,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
