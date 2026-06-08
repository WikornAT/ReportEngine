using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ReportEngine.SharedKernel;

using Templates.Application.DTOs;
using Templates.Application.Features.ReportTemplates.GetAll;
using Templates.Application.Features.ReportTemplates.GetById;
using Templates.Application.Features.ReportTemplates.Publish;
using Templates.Application.Features.ReportTemplates.Upsert;
using Templates.Application.Features.TemplateAssets.GetAssetContent;
using Templates.Application.Features.TemplateAssets.GetByTemplate;
using Templates.Application.Features.TemplateImports.ImportHtml;
using Templates.Application.Features.TemplateImports.ImportZip;

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
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

    // ── Import ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Imports a report template from a multipart/form-data upload containing
    /// an HTML file and optional CSS file plus binary assets.
    /// </summary>
    [HttpPost("import-html")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportHtml(
        [FromForm] ImportHtmlTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        byte[] htmlBytes = await ReadFormFileAsync(request.HtmlFile);
        byte[]? cssBytes = request.CssFile is not null
            ? await ReadFormFileAsync(request.CssFile)
            : null;

        var assetPairs = new Dictionary<string, byte[]>();
        if (request.Assets is not null)
        {
            foreach (IFormFile asset in request.Assets)
            {
                byte[] bytes = await ReadFormFileAsync(asset);
                assetPairs[asset.FileName] = bytes;
            }
        }

        var command = new ImportHtmlTemplateCommand(
            HtmlFileContent: htmlBytes,
            HtmlFileName: request.HtmlFile.FileName,
            CssFileContent: cssBytes,
            CssFileName: request.CssFile?.FileName,
            Assets: assetPairs,
            TemplateCode: request.TemplateCode ?? string.Empty,
            Name: request.Name,
            Description: request.Description,
            PaperSize: request.PaperSize,
            Orientation: request.Orientation,
            WidthPx: request.WidthPx,
            HeightPx: request.HeightPx,
            AllowExternalAssets: request.AllowExternalAssets);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Template.Id }, result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Imports a report template from a ZIP archive that contains
    /// <c>template.html</c> at the root and optional assets.
    /// </summary>
    [HttpPost("import-zip")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportZip(
        [FromForm] ImportZipTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        byte[] zipBytes = await ReadFormFileAsync(request.ZipFile);

        var command = new ImportZipTemplateCommand(
            ZipFileContent: zipBytes,
            ZipFileName: request.ZipFile.FileName,
            TemplateCode: request.TemplateCode ?? string.Empty,
            Name: request.Name,
            Description: request.Description,
            PaperSize: request.PaperSize,
            Orientation: request.Orientation,
            WidthPx: request.WidthPx,
            HeightPx: request.HeightPx,
            AllowExternalAssets: request.AllowExternalAssets);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Template.Id }, result.Value)
            : MapError(result);
    }

    // ── Assets ────────────────────────────────────────────────────────────────

    /// <summary>Lists all assets registered for a template.</summary>
    [HttpGet("{id:guid}/assets")]
    public async Task<IActionResult> GetAssets(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTemplateAssetsQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result);
    }

    /// <summary>Serves the raw binary content of a template asset.</summary>
    [HttpGet("{id:guid}/assets/{assetId:guid}/content")]
    public async Task<IActionResult> GetAssetContent(
        Guid id,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTemplateAssetContentQuery(id, assetId), cancellationToken);

        if (!result.IsSuccess)
        {
            return MapError(result);
        }

        AssetContentResult asset = result.Value;
        return File(asset.Content, asset.ContentType, asset.FileName);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task<byte[]> ReadFormFileAsync(IFormFile file)
    {
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream);
        return stream.ToArray();
    }    // ── Helpers ───────────────────────────────────────────────────────────────

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

