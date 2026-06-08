using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddRenderLogTimingFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ParametersJson",
            schema: "reporting",
            table: "render_logs",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "DataSourceExecutionMs",
            schema: "reporting",
            table: "render_logs",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "TemplateBindingMs",
            schema: "reporting",
            table: "render_logs",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "RenderMs",
            schema: "reporting",
            table: "render_logs",
            type: "bigint",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ParametersJson",        schema: "reporting", table: "render_logs");
        migrationBuilder.DropColumn(name: "DataSourceExecutionMs", schema: "reporting", table: "render_logs");
        migrationBuilder.DropColumn(name: "TemplateBindingMs",     schema: "reporting", table: "render_logs");
        migrationBuilder.DropColumn(name: "RenderMs",              schema: "reporting", table: "render_logs");
    }
}
