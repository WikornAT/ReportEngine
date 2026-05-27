namespace Labeling.Application.GuaranteeInfo;

/// <summary>
/// Read model for a child GuaranteeDebt record, nested inside <see cref="GuaranteeInfoDto"/>.
/// </summary>
public sealed record GuaranteeDebtDto(
    decimal? DebtIdpk,
    decimal? GrtIdpk,
    string? LetterNo,
    string? CustName,
    decimal? CustId,
    string? CusNameTh,
    string? TypeCust,
    decimal? LimitId,
    string? LimitDesc,
    string? Reference,
    decimal? PrincipleAmount,
    string? InterestAmount,
    string? FeeAmount,
    decimal? Nb,
    string? CurrencyDesc,
    string? DueDate,
    string? DateFrom,
    string? DateTo);
