using Reporting.Application.DTOs;
using Reporting.Domain.ReportDefinitions;
using Reporting.Domain.ReportExecutions;

namespace Reporting.Application.Mapping;

/// <summary>
/// Static mapping extensions that translate Domain aggregates to Application DTOs.
/// Explicit and readable — no reflection-based mapping libraries are used.
/// </summary>
public static class ReportingMappingExtensions
{
    // ── ReportParameter ───────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="ReportParameter"/> entity to <see cref="ReportParameterDto"/>.</summary>
    public static ReportParameterDto ToDto(this ReportParameter parameter) =>
        new(
            Id: parameter.Id,
            ReportDefinitionId: parameter.ReportDefinitionId,
            Name: parameter.Name,
            DisplayName: parameter.DisplayName,
            Description: parameter.Description,
            ParameterType: parameter.ParameterType,
            IsRequired: parameter.IsRequired,
            DefaultValue: parameter.DefaultValue,
            SortOrder: parameter.SortOrder,
            IsVisible: parameter.IsVisible);

    // ── ReportDataSource ──────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="ReportDataSource"/> entity to <see cref="ReportDataSourceDto"/>.</summary>
    public static ReportDataSourceDto ToDto(this ReportDataSource dataSource) =>
        new(
            Id: dataSource.Id,
            ReportDefinitionId: dataSource.ReportDefinitionId,
            Name: dataSource.Name,
            DataSourceType: dataSource.DataSourceType,
            ConnectionStringName: dataSource.ConnectionStringName,
            QueryText: dataSource.QueryText,
            SortOrder: dataSource.SortOrder);

    // ── ReportDefinition ──────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="ReportDefinition"/> aggregate root to <see cref="ReportDefinitionDto"/>.</summary>
    public static ReportDefinitionDto ToDto(this ReportDefinition definition) =>
        new(
            Id: definition.Id,
            Name: definition.Name,
            Description: definition.Description,
            Category: definition.Category,
            SubCategory: definition.SubCategory,
            TemplateId: definition.TemplateId,
            TemplatePath: definition.TemplatePath,
            Status: definition.Status,
            IsHidden: definition.IsHidden,
            ExecutionTimeoutSeconds: definition.ExecutionTimeoutSeconds,
            MaxRowCount: definition.MaxRowCount,
            Parameters: definition.Parameters.Select(p => p.ToDto()).ToList(),
            DataSources: definition.DataSources.Select(ds => ds.ToDto()).ToList(),
            CreatedAt: definition.CreatedAt,
            CreatedBy: definition.CreatedBy,
            ModifiedAt: definition.ModifiedAt,
            ModifiedBy: definition.ModifiedBy);

    // ── ReportOutputFile ──────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="ReportOutputFile"/> entity to <see cref="ReportOutputFileDto"/>.</summary>
    public static ReportOutputFileDto ToDto(this ReportOutputFile file) =>
        new(
            Id: file.Id,
            ReportExecutionId: file.ReportExecutionId,
            OutputFormat: file.OutputFormat,
            FileName: file.FileName,
            StoragePath: file.StoragePath,
            ContentType: file.ContentType,
            FileSizeBytes: file.FileSizeBytes,
            GeneratedAt: file.GeneratedAt);

    // ── ReportExecution ───────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="ReportExecution"/> aggregate root to <see cref="ReportExecutionDto"/>.</summary>
    public static ReportExecutionDto ToDto(this ReportExecution execution) =>
        new(
            Id: execution.Id,
            ReportDefinitionId: execution.ReportDefinitionId,
            ReportName: execution.ReportName,
            ParametersJson: execution.ParametersJson,
            RequestedFormats: execution.RequestedFormats,
            Status: execution.Status,
            StartedAt: execution.StartedAt,
            CompletedAt: execution.CompletedAt,
            DurationMs: execution.DurationMs,
            ErrorMessage: execution.ErrorMessage,
            RowCount: execution.RowCount,
            TriggeredBy: execution.TriggeredBy,
            CorrelationId: execution.CorrelationId,
            OutputFiles: execution.OutputFiles.Select(f => f.ToDto()).ToList(),
            CreatedAt: execution.CreatedAt,
            CreatedBy: execution.CreatedBy,
            ModifiedAt: execution.ModifiedAt,
            ModifiedBy: execution.ModifiedBy);
}
