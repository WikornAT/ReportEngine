using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Reporting.Api.Models;
using Reporting.Application.Features.ReportDefinitions.RenderPdf;
using Reporting.Application.Features.ReportDefinitions.RenderPreview;

using ReportEngine.SharedKernel;

namespace Reporting.Api.Controllers;

/// <summary>
/// Versioned v1 report rendering endpoints.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsV1Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsV1Controller(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the merged HTML of a report for designer preview.</summary>
    [HttpPost("{reportId:guid}/preview-html")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PreviewHtml(
        Guid reportId,
        [FromBody] RenderReportRequest request,
        CancellationToken cancellationToken)
    {
        Result<string> result = await _mediator.Send(
            new RenderReportPreviewQuery(
                ReportDefinitionId: reportId,
                ParametersJson: request.ParametersJson ?? "{}",
                TriggeredBy: request.TriggeredBy ?? User.Identity?.Name ?? "anonymous"),
            cancellationToken);

        return result.IsSuccess
            ? Content(result.Value, "text/html; charset=utf-8")
            : Problem(result);
    }

    /// <summary>Renders a report to PDF and returns the raw bytes.</summary>
    [HttpPost("{reportId:guid}/render-pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RenderPdf(
        Guid reportId,
        [FromBody] RenderReportRequest request,
        CancellationToken cancellationToken)
    {
        Result<byte[]> result = await _mediator.Send(
            new RenderReportPdfQuery(
                ReportDefinitionId: reportId,
                ParametersJson: request.ParametersJson ?? "{}",
                TriggeredBy: request.TriggeredBy ?? User.Identity?.Name ?? "anonymous"),
            cancellationToken);

        return result.IsSuccess
            ? File(result.Value, "application/pdf", $"report_{reportId:N}.pdf")
            : Problem(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ObjectResult Problem<T>(Result<T> result)
    {
        AppError error = result.Error;
        int statusCode = error.Code switch
        {
            "Conflict"        => StatusCodes.Status409Conflict,
            "Validation"      => StatusCodes.Status422UnprocessableEntity,
            "DomainViolation" => StatusCodes.Status422UnprocessableEntity,
            _ when error.Code.EndsWith(".NotFound", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            _                 => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: error.Message, title: error.Code, statusCode: statusCode);
    }
}
