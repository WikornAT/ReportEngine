namespace Labeling.Application.GuaranteeInfo;

/// <summary>
/// Read model returned by the GuaranteeInfo service operations.
/// </summary>
public sealed record GuaranteeInfoDto(
    decimal? GrtIdpk,
    string? LetterNo,
    string? GrtName,
    string? AddrNo,
    string? District,
    string? AddressProvince,
    string? CustName,
    decimal? CustId,
    string? CusNameTh,
    string? TypeCust,
    decimal? LimitId,
    string? LimitDesc,
    string? ContractSignDate,
    string? DateNow,
    string? ConDate,
    IReadOnlyList<GuaranteeDebtDto> GuaranteeDebts);
