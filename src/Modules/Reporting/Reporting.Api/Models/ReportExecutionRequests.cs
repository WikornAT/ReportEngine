using Reporting.Domain.Enums;

namespace Reporting.Api.Models;

/// <summary>Request body for POST /api/reporting/executions</summary>
public sealed record ExecuteReportRequest(
    Guid ReportDefinitionId,
    string ParametersJson,
    IReadOnlyList<ReportOutputFormat> RequestedFormats,
    string? CorrelationId);

/// <summary>Request body for POST /api/reporting/report-definitions/{id}/render-pdf</summary>
public sealed record RenderReportPdfRequest(string ParametersJson = "{}");
