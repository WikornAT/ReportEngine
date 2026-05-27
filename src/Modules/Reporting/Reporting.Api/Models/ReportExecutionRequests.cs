using Reporting.Domain.Enums;

namespace Reporting.Api.Models;

/// <summary>Request body for POST /api/reporting/executions</summary>
public sealed record ExecuteReportRequest(
    Guid ReportDefinitionId,
    string ParametersJson,
    IReadOnlyList<ReportOutputFormat> RequestedFormats,
    string? CorrelationId);
