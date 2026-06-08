using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddRenderLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "render_logs",
            schema: "reporting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DurationMs = table.Column<long>(type: "bigint", nullable: true),
                OutputSizeBytes = table.Column<int>(type: "integer", nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                TriggeredBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_render_logs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_render_logs_ReportDefinitionId",
            schema: "reporting",
            table: "render_logs",
            column: "ReportDefinitionId");

        migrationBuilder.CreateIndex(
            name: "IX_render_logs_StartedAt",
            schema: "reporting",
            table: "render_logs",
            column: "StartedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "render_logs",
            schema: "reporting");
    }
}
