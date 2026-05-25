using Microsoft.EntityFrameworkCore.Migrations;

namespace Labeling.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "T4D_DEV");

        migrationBuilder.CreateTable(
            name: "guarantee_debt",
            schema: "T4D_DEV",
            columns: table => new
            {
                debt_idpk = table.Column<decimal>(type: "numeric", nullable: false),
                grt_idpk = table.Column<decimal>(type: "numeric", nullable: true),
                letter_no = table.Column<string>(type: "character varying(67)", maxLength: 67, nullable: true),
                custname = table.Column<string>(type: "character varying(68)", maxLength: 68, nullable: true),
                cust_id = table.Column<decimal>(type: "numeric", nullable: true),
                cus_name_th = table.Column<string>(type: "character varying(61)", maxLength: 61, nullable: true),
                typecust = table.Column<string>(type: "character varying(51)", maxLength: 51, nullable: true),
                limit_id = table.Column<decimal>(type: "numeric", nullable: true),
                limit_desc = table.Column<string>(type: "character varying(74)", maxLength: 74, nullable: true),
                reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                principle_amount = table.Column<decimal>(type: "numeric", nullable: true),
                interest_amount = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                fee_amount = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                nb = table.Column<decimal>(type: "numeric", nullable: true),
                currency_desc = table.Column<string>(type: "character varying(53)", maxLength: 53, nullable: true),
                due_date = table.Column<string>(type: "character varying(69)", maxLength: 69, nullable: true),
                date_from = table.Column<string>(type: "character varying(69)", maxLength: 69, nullable: true),
                date_to = table.Column<string>(type: "character varying(69)", maxLength: 69, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_guarantee_debt", x => x.debt_idpk);
            });

        migrationBuilder.CreateTable(
            name: "guarantee_info",
            schema: "T4D_DEV",
            columns: table => new
            {
                grt_idpk = table.Column<decimal>(type: "numeric", nullable: false),
                letter_no = table.Column<string>(type: "character varying(67)", maxLength: 67, nullable: true),
                grt_name = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                addr_no = table.Column<string>(type: "character varying(65)", maxLength: 65, nullable: true),
                district = table.Column<string>(type: "character varying(68)", maxLength: 68, nullable: true),
                address_province = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                custname = table.Column<string>(type: "character varying(65)", maxLength: 65, nullable: true),
                cust_id = table.Column<decimal>(type: "numeric", nullable: true),
                cus_name_th = table.Column<string>(type: "character varying(61)", maxLength: 61, nullable: true),
                typecust = table.Column<string>(type: "character varying(51)", maxLength: 51, nullable: true),
                limit_id = table.Column<decimal>(type: "numeric", nullable: true),
                limit_desc = table.Column<string>(type: "character varying(74)", maxLength: 74, nullable: true),
                contract_sign_date = table.Column<string>(type: "character varying(65)", maxLength: 65, nullable: true),
                datenow = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                con_date = table.Column<string>(type: "character varying(69)", maxLength: 69, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_guarantee_info", x => x.grt_idpk);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "guarantee_debt",
            schema: "T4D_DEV");

        migrationBuilder.DropTable(
            name: "guarantee_info",
            schema: "T4D_DEV");
    }
}
