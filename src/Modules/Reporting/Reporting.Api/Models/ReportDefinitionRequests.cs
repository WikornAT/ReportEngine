using System.Text.Json;
using System.Text.Json.Serialization;

using Reporting.Domain.Enums;

namespace Reporting.Api.Models;

/// <summary>Request body for POST /api/reporting/report-definitions</summary>
public sealed record CreateReportDefinitionRequest(
    string Name,
    string Category,
    string? Description,
    string? SubCategory);

/// <summary>Request body for PUT /api/reporting/report-definitions/{id}</summary>
public sealed record UpdateReportDefinitionRequest(
    string Name,
    string Category,
    string? Description,
    string? SubCategory);

/// <summary>Request body for POST /api/reporting/report-definitions/{id}/data-sources</summary>
public sealed record AddReportDataSourceRequest(
    string Name,
    ReportDataSourceType DataSourceType,
    string? ConnectionStringName,
    string? QueryText,
    int SortOrder);

/// <summary>Request body for PUT /api/reporting/report-definitions/{id}/data-sources/{dataSourceId}</summary>
public sealed record UpdateReportDataSourceRequest(
    string Name,
    ReportDataSourceType DataSourceType,
    string? ConnectionStringName,
    string? QueryText,
    int SortOrder);

/// <summary>Request body for POST /api/reporting/report-definitions/{id}/parameters</summary>
public sealed record AddReportParameterRequest(
    string Name,
    string DisplayName,
    ReportParameterType ParameterType,
    bool IsRequired,
    string? DefaultValue,
    int SortOrder,
    bool IsVisible,
    string? Description);

/// <summary>Request body for POST /api/reporting/report-definitions/{id}/assign-template</summary>
public sealed record AssignTemplateRequest(
    Guid TemplateId);

/// <summary>Request body for POST /api/reporting/report-definitions/{id}/data-sources/{dsId}/parameters</summary>
public sealed record AddDataSourceParameterRequest(
    string SourceParameterName,
    string ReportParameterName,
    string? DbType,
    bool IsRequired,
    string? DefaultValue);

/// <summary>Request body for PUT /api/reporting/report-definitions/{id}/data-sources/{dsId}/parameters/{parameterId}</summary>
public sealed record UpdateDataSourceParameterRequest(
    string SourceParameterName,
    string ReportParameterName,
    string? DbType,
    bool IsRequired,
    string? DefaultValue);

/// <summary>
/// Request body for POST /api/v1/reports/{reportId}/preview-html
/// and POST /api/v1/reports/{reportId}/render-pdf.
/// </summary>
/// <param name="ParametersJson">JSON object of parameter values, e.g. {"invoiceNo":"INV-001"}. Defaults to empty object.</param>
/// <param name="TriggeredBy">Identity of the caller for audit log. Defaults to the authenticated user name.</param>
public sealed record RenderReportRequest(
    string? ParametersJson = "{}",
    string? TriggeredBy = null);

/// <summary>
/// Request body for POST /api/v1/reports/{reportId}/render.
/// <para>
/// <c>parametersJsonItems</c> is an ordered array of parameter objects, one per logical document.
/// Each element is a JSON object (e.g. <c>{"invoiceNo":"INV-001"}</c>).
/// </para>
/// <para>Example — Single:</para>
/// <code>{ "renderMode": "Single", "parametersJsonItems": [{"invoiceNo":"INV-001"}] }</code>
/// <para>Example — MergePdf:</para>
/// <code>{ "renderMode": "MergePdf", "parametersJsonItems": [{"invoiceNo":"INV-001"},{"invoiceNo":"INV-002"}] }</code>
/// <para>Example — ZipPdf:</para>
/// <code>{ "renderMode": "ZipPdf", "parametersJsonItems": [...], "continueOnError": true }</code>
/// <para>Example — PreviewHtml:</para>
/// <code>{ "renderMode": "PreviewHtml", "parametersJsonItems": [{"invoiceNo":"INV-001"}] }</code>
/// </summary>
public sealed record RenderBatchRequest(
    [property: JsonPropertyName("renderMode")]
    RenderMode RenderMode,
    [property: JsonPropertyName("parametersJsonItems")]
    IReadOnlyList<JsonElement>? ParametersJsonItems = null,
    [property: JsonPropertyName("outputFileNamePattern")]
    string? OutputFileNamePattern = null,
    [property: JsonPropertyName("continueOnError")]
    bool ContinueOnError = false,
    [property: JsonPropertyName("triggeredBy")]
    string? TriggeredBy = null)
{
    /// <summary>
    /// Converts each <see cref="JsonElement"/> item to a raw JSON string for downstream processing.
    /// Returns an empty collection when <see cref="ParametersJsonItems"/> is null.
    /// </summary>
    public IReadOnlyList<string> ParametersJsonStrings =>
        ParametersJsonItems is null
            ? []
            : ParametersJsonItems
                .Select(e => e.ValueKind == JsonValueKind.Undefined ? "{}" : e.GetRawText())
                .ToList();
}
