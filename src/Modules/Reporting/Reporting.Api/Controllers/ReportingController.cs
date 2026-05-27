using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Reporting.Api.Models;
using Reporting.Application.Features.ReportDefinitions.Activate;
using Reporting.Application.Features.ReportDefinitions.AddDataSource;
using Reporting.Application.Features.ReportDefinitions.AddParameter;
using Reporting.Application.Features.ReportDefinitions.AssignTemplate;
using Reporting.Application.Features.ReportDefinitions.Create;
using Reporting.Application.Features.ReportDefinitions.Deactivate;
using Reporting.Application.Features.ReportDefinitions.GetById;
using Reporting.Application.Features.ReportDefinitions.GetList;
using Reporting.Application.Features.ReportDefinitions.Update;
using Reporting.Application.Features.ReportExecutions.Execute;
using Reporting.Application.Features.ReportExecutions.GetHistory;
using Reporting.Domain.Enums;

using Exim.ReportEngine.SharedKernel;

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
            new AssignTemplateCommand(id, request.TemplateId, request.TemplatePath),
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

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
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
