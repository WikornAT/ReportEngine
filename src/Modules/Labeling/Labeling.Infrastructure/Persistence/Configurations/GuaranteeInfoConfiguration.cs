using Labeling.Domain.GuaranteeInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuaranteeDebtEntity = Labeling.Domain.GuaranteeDebt.GuaranteeDebt;

namespace Labeling.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for GuaranteeInfo → T4D_DEV.guarantee_info.
/// All columns are nullable as per the DDL.
/// PKs in legacy tables are treated as externally assigned (no ValueGeneratedOnAdd).
/// </summary>
internal sealed class GuaranteeInfoConfiguration : IEntityTypeConfiguration<GuaranteeInfo>
{
    public void Configure(EntityTypeBuilder<GuaranteeInfo> builder)
    {
        builder.ToTable("guarantee_info", schema: "T4D_DEV");

        // ── Primary key ────────────────────────────────────────────────────
        // GrtIdpk is nullable decimal in the DDL; EF Core requires a non-null key shadow
        // property or a workaround. We configure it as the key and allow null at DB level.
        builder.HasKey(x => x.GrtIdpk);

        builder.Property(x => x.GrtIdpk)
               .HasColumnName("grt_idpk")
               .HasColumnType("numeric")
               .ValueGeneratedNever();

        // ── Letter / header ────────────────────────────────────────────────
        builder.Property(x => x.LetterNo)
               .HasColumnName("letter_no")
               .HasMaxLength(67);

        builder.Property(x => x.GrtName)
               .HasColumnName("grt_name")
               .HasMaxLength(75);

        // ── Address ────────────────────────────────────────────────────────
        builder.Property(x => x.AddrNo)
               .HasColumnName("addr_no")
               .HasMaxLength(65);

        builder.Property(x => x.District)
               .HasColumnName("district")
               .HasMaxLength(68);

        builder.Property(x => x.AddressProvince)
               .HasColumnName("address_province")
               .HasMaxLength(70);

        // ── Customer ───────────────────────────────────────────────────────
        builder.Property(x => x.CustName)
               .HasColumnName("custname")
               .HasMaxLength(65);

        builder.Property(x => x.CustId)
               .HasColumnName("cust_id")
               .HasColumnType("numeric");

        builder.Property(x => x.CusNameTh)
               .HasColumnName("cus_name_th")
               .HasMaxLength(61);

        builder.Property(x => x.TypeCust)
               .HasColumnName("typecust")
               .HasMaxLength(51);

        // ── Limit ──────────────────────────────────────────────────────────
        builder.Property(x => x.LimitId)
               .HasColumnName("limit_id")
               .HasColumnType("numeric");

        builder.Property(x => x.LimitDesc)
               .HasColumnName("limit_desc")
               .HasMaxLength(74);

        // ── Dates (stored as varchar) ──────────────────────────────────────
        builder.Property(x => x.ContractSignDate)
               .HasColumnName("contract_sign_date")
               .HasMaxLength(65);

        builder.Property(x => x.DateNow)
               .HasColumnName("datenow")
               .HasMaxLength(64);

        builder.Property(x => x.ConDate)
               .HasColumnName("con_date")
               .HasMaxLength(69);

        // ── Navigation: child debt lines ───────────────────────────────────
        builder.HasMany<GuaranteeDebtEntity>(x => x.GuaranteeDebts)
               .WithOne()
               .HasForeignKey(x => x.GrtIdpk)
               .HasPrincipalKey(x => x.GrtIdpk)
               .IsRequired(false);
    }
}
