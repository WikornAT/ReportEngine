using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ReportEngine.SharedKernel;

using Templates.Application.DTOs;
using Templates.Application.Features.ReportTemplates.GetAll;
using Templates.Application.Features.ReportTemplates.GetById;
using Templates.Application.Features.ReportTemplates.Publish;
using Templates.Application.Features.ReportTemplates.Upsert;

namespace Templates.Api.Controllers;

[ApiController]
[Route("api/templates/report-templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Ping ──────────────────────────────────────────────────────────────────

    [HttpGet("/api/templates/ping")]
    public IActionResult Ping() =>
        Ok(new { Module = "Templates", Status = "ok", Timestamp = DateTimeOffset.UtcNow });

    // ── Report Templates ──────────────────────────────────────────────────────

    /// <summary>Returns all report templates.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllReportTemplatesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result);
    }

    /// <summary>Returns a single report template by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportTemplateByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result);
    }

    /// <summary>Creates a new report template.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertReportTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { Id = null }, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : MapError(result);
    }

    /// <summary>Updates an existing report template's content.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertReportTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result);
    }

    /// <summary>Publishes a draft template, making it available for rendering.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new PublishReportTemplateCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result);
    }

    /// <summary>Returns the resolved HTML content of a template for preview.</summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportTemplateByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return MapError(result);
        }

        ReportTemplateDto dto = result.Value;
        string html = string.IsNullOrWhiteSpace(dto.CssContent)
            ? dto.HtmlContent
            : dto.HtmlContent.Replace("</head>", $"<style>{dto.CssContent}</style></head>",
                StringComparison.OrdinalIgnoreCase);

        return Content(html, "text/html");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ObjectResult MapError<T>(Result<T> result)
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

        return new ObjectResult(new ProblemDetails { Detail = error.Message, Title = error.Code, Status = statusCode })
        {
            StatusCode = statusCode
        };
    }
}

