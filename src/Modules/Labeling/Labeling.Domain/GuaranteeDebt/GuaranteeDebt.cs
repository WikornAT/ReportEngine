namespace Labeling.Domain.GuaranteeDebt;

/// <summary>
/// Represents a single debt line associated with a guarantee letter,
/// sourced from the legacy guarantee_debt table.
///
/// Modeled as a separate aggregate root (not a child entity of GuaranteeInfo)
/// because:
///   - It carries its own surrogate key (debt_idpk).
///   - It duplicates customer/limit/letter fields, which is characteristic of a
///     denormalized legacy read-model or integration view rather than a true
///     owned child.
///   - In practice, debt records are loaded, paged, and written independently
///     of the parent guarantee header.
///
/// See design summary for the full trade-off analysis.
/// </summary>
public sealed class GuaranteeDebt
{
    // ── Identity ──────────────────────────────────────────────────────────────
    /// <summary>debt_idpk — surrogate primary key for this debt line. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? DebtIdpk { get; private set; }

    // ── Foreign / denormalized guarantee reference ────────────────────────────
    /// <summary>grt_idpk — reference to the parent guarantee_info record. Denormalized; not an ownership key.</summary>
    public decimal? GrtIdpk { get; private set; }

    /// <summary>letter_no — guarantee letter reference number (denormalized from guarantee_info).</summary>
    public string? LetterNo { get; private set; }

    // ── Customer (denormalized) ───────────────────────────────────────────────
    /// <summary>custname — customer name in Latin script (denormalized).</summary>
    public string? CustName { get; private set; }

    /// <summary>cust_id — customer identifier. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? CustId { get; private set; }

    /// <summary>cus_name_th — customer name in Thai script (denormalized).</summary>
    public string? CusNameTh { get; private set; }

    /// <summary>typecust — customer type code (denormalized).</summary>
    public string? TypeCust { get; private set; }

    // ── Limit (denormalized) ──────────────────────────────────────────────────
    /// <summary>limit_id — limit identifier. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? LimitId { get; private set; }

    /// <summary>limit_desc — human-readable limit description (denormalized).</summary>
    public string? LimitDesc { get; private set; }

    // ── Debt financial data ───────────────────────────────────────────────────
    /// <summary>reference — debt or transaction reference code.</summary>
    public string? Reference { get; private set; }

    /// <summary>principle_amount — principal amount. decimal? because NUMERIC precision is unspecified.</summary>
    public decimal? PrincipleAmount { get; private set; }

    /// <summary>interest_amount — stored as VARCHAR in source; keep as string? to avoid silent parse failures.</summary>
    public string? InterestAmount { get; private set; }

    /// <summary>fee_amount — stored as VARCHAR in source; keep as string? to avoid silent parse failures.</summary>
    public string? FeeAmount { get; private set; }

    /// <summary>nb — numeric auxiliary field with unknown domain semantics. decimal? until precision is confirmed.</summary>
    public decimal? Nb { get; private set; }

    /// <summary>currency_desc — currency description or code.</summary>
    public string? CurrencyDesc { get; private set; }

    // ── Dates (stored as VARCHAR in the source table) ─────────────────────────
    /// <summary>due_date — raw string as stored; parse at application layer if needed.</summary>
    public string? DueDate { get; private set; }

    /// <summary>date_from — range start date raw string.</summary>
    public string? DateFrom { get; private set; }

    /// <summary>date_to — range end date raw string.</summary>
    public string? DateTo { get; private set; }

    // ── ORM constructor (EF Core / Dapper — parameterless, private) ───────────
    private GuaranteeDebt() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Creates a new GuaranteeDebt aggregate.
    /// Use this factory method when inserting a new record.
    /// </summary>
    public static GuaranteeDebt Create(
        decimal? debtIdpk,
        decimal? grtIdpk,
        string? letterNo,
        string? custName,
        decimal? custId,
        string? cusNameTh,
        string? typeCust,
        decimal? limitId,
        string? limitDesc,
        string? reference,
        decimal? principleAmount,
        string? interestAmount,
        string? feeAmount,
        decimal? nb,
        string? currencyDesc,
        string? dueDate,
        string? dateFrom,
        string? dateTo)
    {
        return new GuaranteeDebt
        {
            DebtIdpk = debtIdpk,
            GrtIdpk = grtIdpk,
            LetterNo = letterNo,
            CustName = custName,
            CustId = custId,
            CusNameTh = cusNameTh,
            TypeCust = typeCust,
            LimitId = limitId,
            LimitDesc = limitDesc,
            Reference = reference,
            PrincipleAmount = principleAmount,
            InterestAmount = interestAmount,
            FeeAmount = feeAmount,
            Nb = nb,
            CurrencyDesc = currencyDesc,
            DueDate = dueDate,
            DateFrom = dateFrom,
            DateTo = dateTo,
        };
    }

    // ── Domain behaviour ──────────────────────────────────────────────────────

    /// <summary>
    /// Updates the financial amounts on this debt line.
    /// Interest and fee remain strings to preserve the source system format.
    /// </summary>
    public void UpdateAmounts(
        decimal? principleAmount,
        string? interestAmount,
        string? feeAmount)
    {
        PrincipleAmount = principleAmount;
        InterestAmount = interestAmount;
        FeeAmount = feeAmount;
    }

    /// <summary>
    /// Updates the date range and due date on this debt line.
    /// All values remain strings to preserve the legacy storage format.
    /// </summary>
    public void UpdateSchedule(
        string? dueDate,
        string? dateFrom,
        string? dateTo)
    {
        DueDate = dueDate;
        DateFrom = dateFrom;
        DateTo = dateTo;
    }
}
