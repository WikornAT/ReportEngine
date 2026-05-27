using Labeling.Domain.GuaranteeDebt;
using Labeling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Labeling.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository implementation for GuaranteeDebt.
/// Maps to T4D_DEV.guarantee_debt via <see cref="LabelingDbContext"/>.
/// </summary>
internal sealed class GuaranteeDebtRepository : IGuaranteeDebtRepository
{
    private readonly LabelingDbContext _dbContext;

    public GuaranteeDebtRepository(LabelingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<GuaranteeDebt?> GetByIdAsync(decimal? id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeDebts
            .FirstOrDefaultAsync(x => x.DebtIdpk == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeDebt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeDebts
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeDebt>> GetByGrtIdpkAsync(decimal grtIdpk, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeDebts
            .Where(x => x.GrtIdpk == grtIdpk)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeDebt>> GetByLetterNoAsync(string letterNo, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeDebts
            .Where(x => x.LetterNo == letterNo)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeDebt>> GetByCustomerIdAsync(decimal custId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeDebts
            .Where(x => x.CustId == custId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(GuaranteeDebt entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.GuaranteeDebts.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(GuaranteeDebt entity, CancellationToken cancellationToken = default)
    {
        _dbContext.GuaranteeDebts.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(decimal? id, CancellationToken cancellationToken = default)
    {
        GuaranteeDebt? entity = await GetByIdAsync(id, cancellationToken);
        if (entity is not null)
        {
            _dbContext.GuaranteeDebts.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
