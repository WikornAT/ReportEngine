using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Labeling.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_guarantee_debt_grt_idpk",
            schema: "T4D_DEV",
            table: "guarantee_debt",
            column: "grt_idpk");

        migrationBuilder.AddForeignKey(
            name: "FK_guarantee_debt_guarantee_info_grt_idpk",
            schema: "T4D_DEV",
            table: "guarantee_debt",
            column: "grt_idpk",
            principalSchema: "T4D_DEV",
            principalTable: "guarantee_info",
            principalColumn: "grt_idpk");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_guarantee_debt_guarantee_info_grt_idpk",
            schema: "T4D_DEV",
            table: "guarantee_debt");

        migrationBuilder.DropIndex(
            name: "IX_guarantee_debt_grt_idpk",
            schema: "T4D_DEV",
            table: "guarantee_debt");
    }
}
