namespace Labeling.Application.GuaranteeInfo;

/// <summary>
/// Request model for updating the general fields of an existing GuaranteeInfo record.
/// </summary>
public sealed record UpdateGuaranteeInfoRequest(
    string? GrtName,
    string? AddrNo,
    string? District,
    string? AddressProvince,
    string? CustName,
    string? CusNameTh,
    string? TypeCust,
    string? LimitDesc,
    string? ContractSignDate,
    string? ConDate,
    string? DateNow);
