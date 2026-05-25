using GuaranteeDebtEntity = Exim.T4d.Labeling.Domain.GuaranteeDebt.GuaranteeDebt;

namespace Exim.T4d.Labeling.Domain.GuaranteeInfo;

/// <summary>
/// Represents a guarantee letter record sourced from the legacy guarantee_info table.
/// Treated as an aggregate root: it owns its own state and is the sole consistency boundary.
/// No child collections are present in the current schema.
/// </summary>
public sealed class GuaranteeInfo
{
    // ── Identity ────────────────────────────────────────────────────────────
    /// <summary>grt_idpk — surrogate primary key. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? GrtIdpk { get; private set; }

    // ── Letter / Guarantee header ────────────────────────────────────────────
    /// <summary>letter_no — guarantee letter reference number.</summary>
    public string? LetterNo { get; private set; }

    /// <summary>grt_name — guarantee name / title.</summary>
    public string? GrtName { get; private set; }

    // ── Address ──────────────────────────────────────────────────────────────
    /// <summary>addr_no — address number.</summary>
    public string? AddrNo { get; private set; }

    /// <summary>district — district name.</summary>
    public string? District { get; private set; }

    /// <summary>address_province — province of the address.</summary>
    public string? AddressProvince { get; private set; }

    // ── Customer ─────────────────────────────────────────────────────────────
    /// <summary>custname — customer name (Latin).</summary>
    public string? CustName { get; private set; }

    /// <summary>cust_id — customer identifier. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? CustId { get; private set; }

    /// <summary>cus_name_th — customer name in Thai.</summary>
    public string? CusNameTh { get; private set; }

    /// <summary>typecust — customer type code.</summary>
    public string? TypeCust { get; private set; }

    // ── Limit ────────────────────────────────────────────────────────────────
    /// <summary>limit_id — limit identifier. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? LimitId { get; private set; }

    /// <summary>limit_desc — human-readable limit description.</summary>
    public string? LimitDesc { get; private set; }

    // ── Dates (stored as VARCHAR in the source table) ────────────────────────
    /// <summary>contract_sign_date — raw string as stored; parse at application layer if needed.</summary>
    public string? ContractSignDate { get; private set; }

    /// <summary>datenow — snapshot date string as stored in source system.</summary>
    public string? DateNow { get; private set; }

    /// <summary>con_date — contract date raw string.</summary>
    public string? ConDate { get; private set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    private readonly List<GuaranteeDebtEntity> _guaranteeDebts = [];
    /// <summary>Child debt lines associated with this guarantee record.</summary>
    public IReadOnlyList<GuaranteeDebtEntity> GuaranteeDebts => _guaranteeDebts.AsReadOnly();

    // ── ORM constructor (EF Core / Dapper — parameterless, private) ──────────
    private GuaranteeInfo() { }

    // ── Factory / Reconstitution constructor ─────────────────────────────────
    /// <summary>
    /// Creates a new GuaranteeInfo aggregate.
    /// Use this factory method when inserting a new record.
    /// </summary>
    public static GuaranteeInfo Create(
        decimal? grtIdpk,
        string? letterNo,
        string? grtName,
        string? addrNo,
        string? district,
        string? addressProvince,
        string? custName,
        decimal? limitId,
        string? limitDesc,
        string? contractSignDate,
        string? dateNow,
        decimal? custId,
        string? conDate,
        string? cusNameTh,
        string? typeCust)
    {
        return new GuaranteeInfo
        {
            GrtIdpk = grtIdpk,
            LetterNo = letterNo,
            GrtName = grtName,
            AddrNo = addrNo,
            District = district,
            AddressProvince = addressProvince,
            CustName = custName,
            LimitId = limitId,
            LimitDesc = limitDesc,
            ContractSignDate = contractSignDate,
            DateNow = dateNow,
            CustId = custId,
            ConDate = conDate,
            CusNameTh = cusNameTh,
            TypeCust = typeCust,
        };
    }

    // ── Domain behaviour ─────────────────────────────────────────────────────

    /// <summary>
    /// Updates the mutable general fields of the guarantee record.
    /// Date fields remain strings to preserve the source format.
    /// </summary>
    public void UpdateGeneralInfo(
        string? grtName,
        string? addrNo,
        string? district,
        string? addressProvince,
        string? custName,
        string? cusNameTh,
        string? typeCust,
        string? limitDesc,
        string? contractSignDate,
        string? conDate,
        string? dateNow)
    {
        GrtName = grtName;
        AddrNo = addrNo;
        District = district;
        AddressProvince = addressProvince;
        CustName = custName;
        CusNameTh = cusNameTh;
        TypeCust = typeCust;
        LimitDesc = limitDesc;
        ContractSignDate = contractSignDate;
        ConDate = conDate;
        DateNow = dateNow;
    }
}
