using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    private static readonly string[] _dataSourceColumns = ["ReportDefinitionId", "Name"];
    private static readonly string[] _definitionColumns = ["Category", "Name"];
    private static readonly string[] _parameterColumns = ["ReportDefinitionId", "Name"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "report_definitions",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplatePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExecutionTimeoutSeconds = table.Column<int>(type: "integer", nullable: true),
                    MaxRowCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_executions",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    requested_formats = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_data_sources",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataSourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectionStringName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QueryText = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_data_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_data_sources_report_definitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalSchema: "reporting",
                        principalTable: "report_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_parameters",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParameterType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_parameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_parameters_report_definitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalSchema: "reporting",
                        principalTable: "report_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_output_files",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_output_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_output_files_report_executions_ReportExecutionId",
                        column: x => x.ReportExecutionId,
                        principalSchema: "reporting",
                        principalTable: "report_executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_data_sources_ReportDefinitionId_Name",
                schema: "reporting",
                table: "report_data_sources",
                columns: _dataSourceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_definitions_Category_Name",
                schema: "reporting",
                table: "report_definitions",
                columns: _definitionColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_executions_CreatedAt",
                schema: "reporting",
                table: "report_executions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_report_executions_ReportDefinitionId",
                schema: "reporting",
                table: "report_executions",
                column: "ReportDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_report_executions_Status",
                schema: "reporting",
                table: "report_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_report_executions_TriggeredBy",
                schema: "reporting",
                table: "report_executions",
                column: "TriggeredBy");

            migrationBuilder.CreateIndex(
                name: "IX_report_output_files_ReportExecutionId",
                schema: "reporting",
                table: "report_output_files",
                column: "ReportExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_report_parameters_ReportDefinitionId_Name",
                schema: "reporting",
                table: "report_parameters",
                columns: _parameterColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_data_sources",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_output_files",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_parameters",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_executions",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_definitions",
                schema: "reporting");
    }
}
