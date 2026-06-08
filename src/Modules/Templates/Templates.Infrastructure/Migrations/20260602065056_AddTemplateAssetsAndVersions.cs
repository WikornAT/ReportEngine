using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Templates.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTemplateAssetsAndVersions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "template_assets",
            schema: "templates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                AssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                RelativePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                StoragePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                PublicUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_template_assets", x => x.Id);
                table.ForeignKey(
                    name: "FK_template_assets_report_templates_TemplateId",
                    column: x => x.TemplateId,
                    principalSchema: "templates",
                    principalTable: "report_templates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "template_versions",
            schema: "templates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                HtmlContent = table.Column<string>(type: "text", nullable: false),
                CssContent = table.Column<string>(type: "text", nullable: true),
                SnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_template_versions", x => x.Id);
                table.ForeignKey(
                    name: "FK_template_versions_report_templates_TemplateId",
                    column: x => x.TemplateId,
                    principalSchema: "templates",
                    principalTable: "report_templates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_template_assets_TemplateId",
            schema: "templates",
            table: "template_assets",
            column: "TemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_template_assets_TemplateId_RelativePath",
            schema: "templates",
            table: "template_assets",
            columns: new[] { "TemplateId", "RelativePath" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_template_versions_TemplateId",
            schema: "templates",
            table: "template_versions",
            column: "TemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_template_versions_TemplateId_Version",
            schema: "templates",
            table: "template_versions",
            columns: new[] { "TemplateId", "Version" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "template_assets",
            schema: "templates");

        migrationBuilder.DropTable(
            name: "template_versions",
            schema: "templates");
    }
}
