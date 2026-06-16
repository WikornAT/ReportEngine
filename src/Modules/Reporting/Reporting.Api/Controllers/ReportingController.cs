using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Reporting.Application.Contracts;
using Reporting.Application.Features.ReportDefinitions.RenderBatch;
using Reporting.Api.Models;
using Reporting.Application.Features.ReportDefinitions.Activate;
using Reporting.Application.Features.ReportDefinitions.AddDataSource;
using Reporting.Application.Features.ReportDefinitions.AddParameter;
using Reporting.Application.Features.ReportDefinitions.AssignTemplate;
using Reporting.Application.Features.ReportDefinitions.Create;
using Reporting.Application.Features.ReportDefinitions.Deactivate;
using Reporting.Application.Features.ReportDefinitions.GetById;
using Reporting.Application.Features.ReportDefinitions.GetDataSourceById;
using Reporting.Application.Features.ReportDefinitions.GetDataSources;
using Reporting.Application.Features.ReportDefinitions.GetList;
using Reporting.Application.Features.ReportDefinitions.RemoveDataSource;
using Reporting.Application.Features.ReportDefinitions.AddDataSourceParameter;
using Reporting.Application.Features.ReportDefinitions.UpdateDataSourceParameter;
using Reporting.Application.Features.ReportDefinitions.RemoveDataSourceParameter;
using Reporting.Application.Features.ReportDefinitions.Update;
using Reporting.Application.Features.ReportDefinitions.UpdateDataSource;
using Reporting.Application.Features.ReportDefinitions.RenderPdf;
using Reporting.Application.Features.ReportDefinitions.RenderPreview;
using Reporting.Application.Features.ReportExecutions.Execute;
using Reporting.Application.Features.ReportExecutions.GetHistory;
using Reporting.Domain.Enums;

using ReportEngine.SharedKernel;

namespace Reporting.Api.Controllers;

[ApiController]
[Route("api/reporting")]
public sealed class ReportingController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Health ────────────────────────────────────────────────────────────────

    [HttpGet("ping")]
    public IActionResult Ping() =>
        Ok(new { Module = "Reporting", Status = "ok", Timestamp = DateTimeOffset.UtcNow });

    // ── Report Definitions ────────────────────────────────────────────────────

    /// <summary>Returns a paged, filtered list of report definitions.</summary>
    [HttpGet("report-definitions")]
    public async Task<IActionResult> GetList(
        [FromQuery] string? category,
        [FromQuery] string? searchTerm,
        [FromQuery] ReportStatus? status,
        [FromQuery] bool includeHidden = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetReportDefinitionsQuery(category, searchTerm, status, includeHidden, page, pageSize),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Returns a single report definition by id, including parameters and data sources.</summary>
    [HttpGet("report-definitions/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportDefinitionByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Creates a new report definition in Draft status.</summary>
    [HttpPost("report-definitions")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CreateReportDefinitionCommand(request.Name, request.Category, request.Description, request.SubCategory),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Problem(result);
    }

    /// <summary>Updates the metadata of an existing report definition.</summary>
    [HttpPut("report-definitions/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateReportDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UpdateReportDefinitionCommand(id, request.Name, request.Category, request.Description, request.SubCategory),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Activates a report definition, allowing it to be executed.</summary>
    [HttpPost("report-definitions/{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ActivateReportDefinitionCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Deactivates a report definition, preventing new executions.</summary>
    [HttpPost("report-definitions/{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeactivateReportDefinitionCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Assigns a template from the Templates module to this report definition.</summary>
    [HttpPost("report-definitions/{id:guid}/assign-template")]
    public async Task<IActionResult> AssignTemplate(
        Guid id,
        [FromBody] AssignTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new AssignTemplateCommand(id, request.TemplateId),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Adds a data source to an existing report definition.</summary>
    [HttpPost("report-definitions/{id:guid}/data-sources")]
    public async Task<IActionResult> AddDataSource(
        Guid id,
        [FromBody] AddReportDataSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new AddReportDataSourceCommand(
                id,
                request.Name,
                request.DataSourceType,
                request.ConnectionStringName,
                request.QueryText,
                request.SortOrder),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDataSourceById), new { id, dataSourceId = result.Value.Id }, result.Value)
            : Problem(result);
    }

    /// <summary>Returns all data sources for a report definition, ordered by SortOrder.</summary>
    [HttpGet("report-definitions/{id:guid}/data-sources")]
    public async Task<IActionResult> GetDataSources(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportDataSourcesQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Returns a single data source by id.</summary>
    [HttpGet("report-definitions/{id:guid}/data-sources/{dataSourceId:guid}")]
    public async Task<IActionResult> GetDataSourceById(
        Guid id,
        Guid dataSourceId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportDataSourceByIdQuery(id, dataSourceId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Updates all mutable fields of an existing data source.</summary>
    [HttpPut("report-definitions/{id:guid}/data-sources/{dataSourceId:guid}")]
    public async Task<IActionResult> UpdateDataSource(
        Guid id,
        Guid dataSourceId,
        [FromBody] UpdateReportDataSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UpdateReportDataSourceCommand(
                id,
                dataSourceId,
                request.Name,
                request.DataSourceType,
                request.ConnectionStringName,
                request.QueryText,
                request.SortOrder),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Removes a data source from a report definition.</summary>
    [HttpDelete("report-definitions/{id:guid}/data-sources/{dataSourceId:guid}")]
    public async Task<IActionResult> RemoveDataSource(
        Guid id,
        Guid dataSourceId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new RemoveReportDataSourceCommand(id, dataSourceId), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    /// <summary>Adds a parameter mapping to a data source (maps SQL/SP param name to report parameter).</summary>
    [HttpPost("report-definitions/{id:guid}/data-sources/{dataSourceId:guid}/parameters")]
    public async Task<IActionResult> AddDataSourceParameter(
        Guid id,
        Guid dataSourceId,
        [FromBody] AddDataSourceParameterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new AddDataSourceParameterCommand(
                id,
                dataSourceId,
                request.SourceParameterName,
                request.ReportParameterName,
                request.DbType,
                request.IsRequired,
                request.DefaultValue),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Updates an existing parameter mapping on a data source.</summary>
    [HttpPut("report-definitions/{id:guid}/data-sources/{dataSourceId:guid}/parameters/{parameterId:guid}")]
    public async Task<IActionResult> UpdateDataSourceParameter(
        Guid id,
        Guid dataSourceId,
        Guid parameterId,
        [FromBody] UpdateDataSourceParameterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UpdateDataSourceParameterCommand(
                id,
                dataSourceId,
                parameterId,
                request.SourceParameterName,
                request.ReportParameterName,
                request.DbType,
                request.IsRequired,
                request.DefaultValue),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Removes a parameter mapping from a data source.</summary>
    [HttpDelete("report-definitions/{id:guid}/data-sources/{dataSourceId:guid}/parameters/{parameterId:guid}")]
    public async Task<IActionResult> RemoveDataSourceParameter(
        Guid id,
        Guid dataSourceId,
        Guid parameterId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RemoveDataSourceParameterCommand(id, dataSourceId, parameterId),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    /// <summary>Declares a new input parameter on an existing report definition.</summary>
    [HttpPost("report-definitions/{id:guid}/parameters")]
    public async Task<IActionResult> AddParameter(
        Guid id,
        [FromBody] AddReportParameterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new AddReportParameterCommand(
                id,
                request.Name,
                request.DisplayName,
                request.ParameterType,
                request.IsRequired,
                request.DefaultValue,
                request.SortOrder,
                request.IsVisible,
                request.Description),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    // ── Report Preview / Direct Render ────────────────────────────────────────

    /// <summary>
    /// Returns the merged HTML of a report definition with live data substituted.
    /// Does not persist a ReportExecution record.
    /// Pass parametersJson as a query-string encoded JSON object, e.g. <c>%7B%22invoiceNo%22%3A%22INV-001%22%7D</c>.
    /// </summary>
    [HttpGet("report-definitions/{id:guid}/preview")]
    public async Task<IActionResult> PreviewHtml(
        Guid id,
        [FromQuery] string parametersJson = "{}",
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RenderReportPreviewQuery(id, parametersJson),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result);
        }

        return Content(result.Value, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Renders a report definition directly to PDF and streams the bytes back.
    /// Does not persist a ReportExecution record.
    /// </summary>
    [HttpPost("report-definitions/{id:guid}/render-pdf")]
    public async Task<IActionResult> RenderPdf(
        Guid id,
        [FromBody] RenderReportPdfRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RenderReportPdfQuery(id, request.ParametersJsonString),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result);
        }

        return File(result.Value, "application/pdf", $"report_{id:N}.pdf");
    }

    // ── Report Executions ─────────────────────────────────────────────────────

    /// <summary>Triggers end-to-end execution of a report.</summary>
    [HttpPost("executions")]
    public async Task<IActionResult> Execute(
        [FromBody] ExecuteReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ExecuteReportCommand(
                request.ReportDefinitionId,
                request.ParametersJson,
                request.RequestedFormats,
                request.CorrelationId),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetExecutionHistory), new { reportDefinitionId = result.Value.ReportDefinitionId }, result.Value)
            : Problem(result);
    }

    /// <summary>Returns paged execution history with optional filters.</summary>
    [HttpGet("executions")]
    public async Task<IActionResult> GetExecutionHistory(
        [FromQuery] Guid? reportDefinitionId,
        [FromQuery] string? triggeredBy,
        [FromQuery] ReportExecutionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetReportExecutionsQuery(reportDefinitionId, triggeredBy, status, page, pageSize),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>
    /// Unified batch render endpoint.
    /// Returns <c>application/pdf</c> for <c>Single</c> and <c>MergePdf</c>,
    /// <c>application/zip</c> for <c>ZipPdf</c>, and <c>text/html</c> for <c>PreviewHtml</c>.
    /// </summary>
    [HttpPost("{reportId:guid}/render")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Render(
        Guid reportId,
        [FromBody] RenderBatchRequest request,
        CancellationToken cancellationToken)
    {
        Result<BatchRenderResult> result = await _mediator.Send(
            new RenderBatchReportCommand(
                ReportDefinitionId: reportId,
                RenderMode: request.RenderMode,
                ParametersJsonItems: request.ParametersJsonStrings,
                OutputFileNamePattern: request.OutputFileNamePattern,
                ContinueOnError: request.ContinueOnError,
                TriggeredBy: request.TriggeredBy ?? User.Identity?.Name ?? "anonymous"),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result);
        }

        BatchRenderResult batch = result.Value;

        return batch.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
            ? Content(System.Text.Encoding.UTF8.GetString(batch.Content), batch.ContentType)
            : File(batch.Content, batch.ContentType, batch.FileName);
    }


    // ── Helpers ───────────────────────────────────────────────────────────────

    private ObjectResult Problem<T>(Result<T> result)
    {
        AppError error = result.Error;
        int statusCode = error.Code switch
        {
            "Conflict"       => StatusCodes.Status409Conflict,
            "Validation"     => StatusCodes.Status422UnprocessableEntity,
            "DomainViolation" => StatusCodes.Status422UnprocessableEntity,
            _ when error.Code.EndsWith(".NotFound", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            _                => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: error.Message, title: error.Code, statusCode: statusCode);
    }
}
