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
    string ConnectionStringName,
    string QueryText,
    int SortOrder);

/// <summary>Request body for PUT /api/reporting/report-definitions/{id}/data-sources/{dataSourceId}</summary>
public sealed record UpdateReportDataSourceRequest(
    string Name,
    ReportDataSourceType DataSourceType,
    string ConnectionStringName,
    string QueryText,
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
    Guid TemplateId,
    string TemplatePath);

/// <summary>
/// Request body for POST /api/v1/reports/{reportId}/preview-html
/// and POST /api/v1/reports/{reportId}/render-pdf.
/// </summary>
/// <param name="ParametersJson">JSON object of parameter values, e.g. {"invoiceNo":"INV-001"}. Defaults to empty object.</param>
/// <param name="TriggeredBy">Identity of the caller for audit log. Defaults to the authenticated user name.</param>
public sealed record RenderReportRequest(
    string? ParametersJson = "{}",
    string? TriggeredBy = null);

