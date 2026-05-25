using Exim.T4d.Labeling.Domain.GuaranteeDebt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Labeling.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for GuaranteeDebt → T4D_DEV.guarantee_debt.
/// All columns are nullable as per the DDL.
/// PKs in legacy tables are treated as externally assigned (no ValueGeneratedOnAdd).
/// </summary>
internal sealed class GuaranteeDebtConfiguration : IEntityTypeConfiguration<GuaranteeDebt>
{
    public void Configure(EntityTypeBuilder<GuaranteeDebt> builder)
    {
        builder.ToTable("guarantee_debt", schema: "T4D_DEV");

        // ── Primary key ────────────────────────────────────────────────────
        builder.HasKey(x => x.DebtIdpk);

        builder.Property(x => x.DebtIdpk)
               .HasColumnName("debt_idpk")
               .HasColumnType("numeric")
               .ValueGeneratedNever();

        // ── Guarantee reference (correlation key) ──────────────────────────
        builder.Property(x => x.GrtIdpk)
               .HasColumnName("grt_idpk")
               .HasColumnType("numeric");

        builder.Property(x => x.LetterNo)
               .HasColumnName("letter_no")
               .HasMaxLength(67);

        // ── Customer (denormalized) ────────────────────────────────────────
        builder.Property(x => x.CustName)
               .HasColumnName("custname")
               .HasMaxLength(68);

        builder.Property(x => x.CustId)
               .HasColumnName("cust_id")
               .HasColumnType("numeric");

        builder.Property(x => x.CusNameTh)
               .HasColumnName("cus_name_th")
               .HasMaxLength(61);

        builder.Property(x => x.TypeCust)
               .HasColumnName("typecust")
               .HasMaxLength(51);

        // ── Limit (denormalized) ───────────────────────────────────────────
        builder.Property(x => x.LimitId)
               .HasColumnName("limit_id")
               .HasColumnType("numeric");

        builder.Property(x => x.LimitDesc)
               .HasColumnName("limit_desc")
               .HasMaxLength(74);

        // ── Debt financial data ────────────────────────────────────────────
        builder.Property(x => x.Reference)
               .HasColumnName("reference")
               .HasMaxLength(64);

        builder.Property(x => x.PrincipleAmount)
               .HasColumnName("principle_amount")
               .HasColumnType("numeric");

        // interest_amount and fee_amount are varchar in the DDL despite their names
        builder.Property(x => x.InterestAmount)
               .HasColumnName("interest_amount")
               .HasMaxLength(54);

        builder.Property(x => x.FeeAmount)
               .HasColumnName("fee_amount")
               .HasMaxLength(54);

        builder.Property(x => x.Nb)
               .HasColumnName("nb")
               .HasColumnType("numeric");

        builder.Property(x => x.CurrencyDesc)
               .HasColumnName("currency_desc")
               .HasMaxLength(53);

        // ── Dates (stored as varchar) ──────────────────────────────────────
        builder.Property(x => x.DueDate)
               .HasColumnName("due_date")
               .HasMaxLength(69);

        builder.Property(x => x.DateFrom)
               .HasColumnName("date_from")
               .HasMaxLength(69);

        builder.Property(x => x.DateTo)
               .HasColumnName("date_to")
               .HasMaxLength(69);
    }
}
