namespace Labeling.Application.GuaranteeInfo;

/// <summary>
/// Application service contract for GuaranteeInfo CRUD operations.
/// </summary>
public interface IGuaranteeInfoService
{
    /// <summary>Returns all guarantee records.</summary>
    public Task<IReadOnlyList<GuaranteeInfoDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single record by its surrogate key, or null if not found.</summary>
    public Task<GuaranteeInfoDto?> GetByIdAsync(decimal? id, CancellationToken cancellationToken = default);

    /// <summary>Returns a single record by letter number, or null if not found.</summary>
    public Task<GuaranteeInfoDto?> GetByLetterNoAsync(string letterNo, CancellationToken cancellationToken = default);

    /// <summary>Returns all records for a given customer identifier.</summary>
    public Task<IReadOnlyList<GuaranteeInfoDto>> GetByCustomerIdAsync(decimal customerId, CancellationToken cancellationToken = default);

    /// <summary>Creates a new GuaranteeInfo record and returns its DTO.</summary>
    public Task<GuaranteeInfoDto> CreateAsync(CreateGuaranteeInfoRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates general fields of an existing record. Returns false if not found.</summary>
    public Task<bool> UpdateAsync(decimal? id, UpdateGuaranteeInfoRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a record by its surrogate key. Returns false if not found.</summary>
    public Task<bool> DeleteAsync(decimal? id, CancellationToken cancellationToken = default);
}
