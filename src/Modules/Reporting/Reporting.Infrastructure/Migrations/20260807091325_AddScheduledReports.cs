using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable IDE0161

namespace Reporting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_reports",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequestedFormatsCsv = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_reports_report_definitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalSchema: "reporting",
                        principalTable: "report_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_reports_IsActive",
                schema: "reporting",
                table: "scheduled_reports",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_reports_ReportDefinitionId_ScheduleType",
                schema: "reporting",
                table: "scheduled_reports",
                columns: new[] { "ReportDefinitionId", "ScheduleType" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_reports_ScheduleType",
                schema: "reporting",
                table: "scheduled_reports",
                column: "ScheduleType");
        }

#pragma warning restore IDE0161

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_reports",
                schema: "reporting");
        }
    }
}
