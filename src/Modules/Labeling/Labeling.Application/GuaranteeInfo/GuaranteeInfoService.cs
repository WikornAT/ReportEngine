using Labeling.Domain.GuaranteeInfo;

namespace Labeling.Application.GuaranteeInfo;

/// <summary>
/// Application service implementation for GuaranteeInfo CRUD operations.
/// Delegates persistence to <see cref="IGuaranteeInfoRepository"/>.
/// </summary>
internal sealed class GuaranteeInfoService : IGuaranteeInfoService
{
    private readonly IGuaranteeInfoRepository _repository;

    public GuaranteeInfoService(IGuaranteeInfoRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeInfoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<GuaranteeInfoDto?> GetByIdAsync(decimal? id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    /// <inheritdoc/>
    public async Task<GuaranteeInfoDto?> GetByLetterNoAsync(string letterNo, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByLetterNoAsync(letterNo, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuaranteeInfoDto>> GetByCustomerIdAsync(decimal customerId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<GuaranteeInfoDto> CreateAsync(CreateGuaranteeInfoRequest request, CancellationToken cancellationToken = default)
    {
        var entity = Domain.GuaranteeInfo.GuaranteeInfo.Create(
            request.GrtIdpk,
            request.LetterNo,
            request.GrtName,
            request.AddrNo,
            request.District,
            request.AddressProvince,
            request.CustName,
            request.LimitId,
            request.LimitDesc,
            request.ContractSignDate,
            request.DateNow,
            request.CustId,
            request.ConDate,
            request.CusNameTh,
            request.TypeCust);

        await _repository.AddAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(decimal? id, UpdateGuaranteeInfoRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.UpdateGeneralInfo(
            request.GrtName,
            request.AddrNo,
            request.District,
            request.AddressProvince,
            request.CustName,
            request.CusNameTh,
            request.TypeCust,
            request.LimitDesc,
            request.ContractSignDate,
            request.ConDate,
            request.DateNow);

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(decimal? id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static GuaranteeInfoDto MapToDto(Domain.GuaranteeInfo.GuaranteeInfo e) =>
        new(e.GrtIdpk, e.LetterNo, e.GrtName, e.AddrNo, e.District, e.AddressProvince,
            e.CustName, e.CustId, e.CusNameTh, e.TypeCust, e.LimitId, e.LimitDesc,
            e.ContractSignDate, e.DateNow, e.ConDate,
            e.GuaranteeDebts.Select(d => new GuaranteeDebtDto(
                d.DebtIdpk, d.GrtIdpk, d.LetterNo, d.CustName, d.CustId, d.CusNameTh,
                d.TypeCust, d.LimitId, d.LimitDesc, d.Reference, d.PrincipleAmount,
                d.InterestAmount, d.FeeAmount, d.Nb, d.CurrencyDesc,
                d.DueDate, d.DateFrom, d.DateTo)).ToList());
}
