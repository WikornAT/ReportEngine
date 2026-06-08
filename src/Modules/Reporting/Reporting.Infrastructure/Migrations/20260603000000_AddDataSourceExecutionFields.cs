using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataSourceExecutionFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TimeoutSeconds",
            schema: "reporting",
            table: "report_data_sources",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            schema: "reporting",
            table: "report_data_sources",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "report_data_source_parameters",
            schema: "reporting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceParameterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ReportParameterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DbType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_report_data_source_parameters", x => x.Id);
                table.ForeignKey(
                    name: "FK_report_data_source_parameters_report_data_sources_ReportDataSourceId",
                    column: x => x.ReportDataSourceId,
                    principalSchema: "reporting",
                    principalTable: "report_data_sources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_report_data_source_parameters_ReportDataSourceId_SourceParameterName",
            schema: "reporting",
            table: "report_data_source_parameters",
            columns: new[] { "ReportDataSourceId", "SourceParameterName" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "report_data_source_parameters",
            schema: "reporting");

        migrationBuilder.DropColumn(
            name: "TimeoutSeconds",
            schema: "reporting",
            table: "report_data_sources");

        migrationBuilder.DropColumn(
            name: "IsActive",
            schema: "reporting",
            table: "report_data_sources");
    }
}
