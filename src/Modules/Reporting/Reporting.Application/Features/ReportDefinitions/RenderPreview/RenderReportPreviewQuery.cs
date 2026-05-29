using MediatR;

using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderPreview;

/// <summary>
/// Returns the rendered HTML of a report definition merged with the supplied data,
/// without creating a <c>ReportExecution</c> record.
/// Intended for designer preview and quick visual verification.
/// </summary>
/// <param name="ReportDefinitionId">Id of the report definition to preview.</param>
/// <param name="ParametersJson">
/// JSON object of parameter values (e.g. <c>{"invoiceNo":"INV-001"}</c>).
/// Pass <c>{}</c> to render with empty data.
/// </param>
/// <param name="TriggeredBy">Identity of the caller recorded in the render log.</param>
public sealed record RenderReportPreviewQuery(
    Guid ReportDefinitionId,
    string ParametersJson = "{}",
    string TriggeredBy = "system") : IRequest<Result<string>>;
