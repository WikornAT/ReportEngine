using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddParameterValidationRule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ValidationRuleJson",
            schema: "reporting",
            table: "report_parameters",
            type: "jsonb",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ValidationRuleJson",
            schema: "reporting",
            table: "report_parameters");
    }
}
