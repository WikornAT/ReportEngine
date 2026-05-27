namespace Labeling.Application.GuaranteeInfo;

/// <summary>
/// Request model for creating a new GuaranteeInfo record.
/// </summary>
public sealed record CreateGuaranteeInfoRequest(
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
    string? ConDate);
