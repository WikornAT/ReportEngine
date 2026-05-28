namespace ReportEngine.Abstractions.Repositories;

/// <summary>
/// Generic repository abstraction. Specific module repositories extend this contract.
/// </summary>
/// <typeparam name="TEntity">The aggregate/entity type.</typeparam>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IRepository<TEntity, TId>
    where TEntity : class
{
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
}
