using Microsoft.AspNetCore.Http;

using Templates.Domain.Enums;

namespace Templates.Api.Controllers;

/// <summary>Form model for <c>POST /import-html</c>.</summary>
public sealed class ImportHtmlTemplateRequest
{
    public IFormFile HtmlFile { get; set; } = null!;
    public IFormFile? CssFile { get; set; }
    public IFormFileCollection? Assets { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? Description { get; set; }
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public int WidthPx { get; set; }
    public int HeightPx { get; set; }
    public bool AllowExternalAssets { get; set; }
}

/// <summary>Form model for <c>POST /import-zip</c>.</summary>
public sealed class ImportZipTemplateRequest
{
    public IFormFile ZipFile { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? Description { get; set; }
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public int WidthPx { get; set; }
    public int HeightPx { get; set; }
    public bool AllowExternalAssets { get; set; }
}
