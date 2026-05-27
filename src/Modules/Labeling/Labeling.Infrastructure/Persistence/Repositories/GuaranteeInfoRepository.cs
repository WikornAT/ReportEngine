using Labeling.Domain.GuaranteeInfo;
using Labeling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Labeling.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository implementation for GuaranteeInfo.
/// Maps to T4D_DEV.guarantee_info via <see cref="LabelingDbContext"/>.
/// </summary>
internal sealed class GuaranteeInfoRepository : IGuaranteeInfoRepository
{
    private readonly LabelingDbContext _dbContext;

    public GuaranteeInfoRepository(LabelingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeInfos
            .Include(x => x.GuaranteeDebts)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuaranteeInfo?> GetByIdAsync(decimal? id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeInfos
            .Include(x => x.GuaranteeDebts)
            .FirstOrDefaultAsync(x => x.GrtIdpk == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuaranteeInfo?> GetByLetterNoAsync(string letterNo, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeInfos
            .Include(x => x.GuaranteeDebts)
            .FirstOrDefaultAsync(x => x.LetterNo == letterNo, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeInfo>> GetByCustomerIdAsync(decimal customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuaranteeInfos
            .Include(x => x.GuaranteeDebts)
            .Where(x => x.CustId == customerId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(GuaranteeInfo entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.GuaranteeInfos.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(GuaranteeInfo entity, CancellationToken cancellationToken = default)
    {
        _dbContext.GuaranteeInfos.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(decimal? id, CancellationToken cancellationToken = default)
    {
        GuaranteeInfo? entity = await GetByIdAsync(id, cancellationToken);
        if (entity is not null)
        {
            _dbContext.GuaranteeInfos.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
