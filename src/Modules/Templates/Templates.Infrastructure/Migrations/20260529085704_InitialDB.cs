using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Templates.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialDB : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "templates");

        migrationBuilder.CreateTable(
            name: "report_templates",
            schema: "templates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                TemplateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                HtmlContent = table.Column<string>(type: "text", nullable: false),
                CssContent = table.Column<string>(type: "text", nullable: true),
                PaperSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Orientation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                WidthPx = table.Column<int>(type: "integer", nullable: false),
                HeightPx = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_report_templates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_report_templates_Name",
            schema: "templates",
            table: "report_templates",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_report_templates_TemplateCode",
            schema: "templates",
            table: "report_templates",
            column: "TemplateCode",
            unique: true,
            filter: "\"TemplateCode\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "report_templates",
            schema: "templates");
    }
}
