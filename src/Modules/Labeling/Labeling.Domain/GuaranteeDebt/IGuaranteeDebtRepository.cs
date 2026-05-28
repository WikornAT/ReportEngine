using ReportEngine.Abstractions.Repositories;

namespace Labeling.Domain.GuaranteeDebt;

/// <summary>
/// Domain-owned repository contract for GuaranteeDebt.
/// Implementations live in Labeling.Infrastructure.
/// </summary>
public interface IGuaranteeDebtRepository : IRepository<GuaranteeDebt, decimal?>
{
    /// <summary>Returns all debt lines linked to a given guarantee identifier.</summary>
    public Task<IReadOnlyList<GuaranteeDebt>> GetByGrtIdpkAsync(decimal grtIdpk, CancellationToken cancellationToken = default);

    /// <summary>Returns all debt lines for a given letter number.</summary>
    public Task<IReadOnlyList<GuaranteeDebt>> GetByLetterNoAsync(string letterNo, CancellationToken cancellationToken = default);

    /// <summary>Returns all debt lines for a given customer identifier.</summary>
    public Task<IReadOnlyList<GuaranteeDebt>> GetByCustomerIdAsync(decimal custId, CancellationToken cancellationToken = default);
}
