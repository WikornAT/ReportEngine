using EximBank.Abstractions.Repositories;

namespace Exim.T4d.Labeling.Domain.GuaranteeInfo;

/// <summary>
/// Domain-owned repository contract for GuaranteeInfo.
/// Implementations live in Labeling.Infrastructure.
/// </summary>
public interface IGuaranteeInfoRepository : IRepository<GuaranteeInfo, decimal?>
{
    /// <summary>Retrieves a guarantee record by its letter number.</summary>
    public Task<GuaranteeInfo?> GetByLetterNoAsync(string letterNo, CancellationToken cancellationToken = default);

    /// <summary>Returns all guarantee records for a given customer identifier.</summary>
    public Task<IReadOnlyList<GuaranteeInfo>> GetByCustomerIdAsync(decimal customerId, CancellationToken cancellationToken = default);
}
