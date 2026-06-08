using System.Text.Json;
using System.Text.Json.Serialization;

using Reporting.Domain.Enums;

namespace Reporting.Api.Models;

/// <summary>Request body for POST /api/reporting/executions</summary>
public sealed record ExecuteReportRequest(
    Guid ReportDefinitionId,
    string ParametersJson,
    IReadOnlyList<ReportOutputFormat> RequestedFormats,
    string? CorrelationId);

/// <summary>
/// Request body for POST /api/reporting/report-definitions/{id}/render-pdf.
/// <para>
/// <c>parametersJson</c> accepts either a flat JSON object for single-page output:
/// <code>{ "letter_no": "LTR-001", "date": "10-05-2026" }</code>
/// or a JSON array where each element produces one page in the resulting PDF:
/// <code>[ { "letter_no": "LTR-001", "typesOfCreditLimits": [...] }, { ... } ]</code>
/// </para>
/// </summary>
public sealed record RenderReportPdfRequest(
    [property: JsonPropertyName("parametersJson")]
    JsonElement ParametersJson)
{
    /// <summary>Returns the raw JSON string to pass downstream.</summary>
    public string ParametersJsonString =>
        ParametersJson.ValueKind == JsonValueKind.Undefined
            ? "{}"
            : ParametersJson.GetRawText();
}
