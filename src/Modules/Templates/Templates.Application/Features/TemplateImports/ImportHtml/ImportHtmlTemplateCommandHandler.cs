using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ReportEngine.SharedKernel;

using Templates.Application.Contracts;
using Templates.Application.DTOs;
using Templates.Application.Mapping;
using Templates.Domain.Enums;
using Templates.Domain.ReportTemplates;

namespace Templates.Application.Features.TemplateImports.ImportHtml;

internal sealed class ImportHtmlTemplateCommandHandler
    : IRequestHandler<ImportHtmlTemplateCommand, Result<TemplateImportResultDto>>
{
    private static readonly HashSet<string> _allowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".css",
            ".png", ".jpg", ".jpeg", ".svg", ".webp",
            ".ttf", ".otf", ".woff", ".woff2",
        };

    private static readonly Action<ILogger, string, Exception?> _logImporting =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(100, "HtmlTemplateImporting"),
            "Importing HTML template '{Name}'");

    private static readonly Action<ILogger, Guid, string, int, Exception?> _logImported =
        LoggerMessage.Define<Guid, string, int>(
            LogLevel.Information,
            new EventId(101, "HtmlTemplateImported"),
            "HTML template '{Name}' imported as {Id} (v{Version})");

    private readonly ITemplatesDbContext _dbContext;
    private readonly IReportTemplateRepository _repository;
    private readonly ITemplateStorageService _storage;
    private readonly ITemplateHtmlSanitizer _sanitizer;
    private readonly ITemplateAssetReferenceRewriter _rewriter;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ImportHtmlTemplateCommandHandler> _logger;

    public ImportHtmlTemplateCommandHandler(
        ITemplatesDbContext dbContext,
        IReportTemplateRepository repository,
        ITemplateStorageService storage,
        ITemplateHtmlSanitizer sanitizer,
        ITemplateAssetReferenceRewriter rewriter,
        ICurrentUserService currentUser,
        ILogger<ImportHtmlTemplateCommandHandler> logger)
    {
        _dbContext = dbContext;
        _repository = repository;
        _storage = storage;
        _sanitizer = sanitizer;
        _rewriter = rewriter;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<TemplateImportResultDto>> Handle(
        ImportHtmlTemplateCommand request,
        CancellationToken cancellationToken)
    {
        // ── Validate HTML extension ───────────────────────────────────────
        string htmlExt = Path.GetExtension(request.HtmlFileName);
        if (!htmlExt.Equals(".html", StringComparison.OrdinalIgnoreCase) &&
            !htmlExt.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            return AppError.Validation("htmlFile must be an .html or .htm file.");
        }

        // ── Validate asset extensions ─────────────────────────────────────
        foreach (string assetPath in request.Assets.Keys)
        {
            string ext = Path.GetExtension(assetPath);
            if (!_allowedExtensions.Contains(ext))
            {
                return AppError.Validation(
                    $"Asset '{assetPath}' has an unsupported extension '{ext}'.");
            }
        }

        _logImporting(_logger, request.Name, null);

        string userId = _currentUser.UserId;

        // ── Check for duplicate TemplateCode ──────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            bool exists = await _dbContext.ReportTemplates
                .AnyAsync(t => t.TemplateCode == request.TemplateCode, cancellationToken);

            if (exists)
            {
                return AppError.Conflict(
                    $"A template with code '{request.TemplateCode}' already exists.");
            }
        }

        // ── Create template aggregate ─────────────────────────────────────
        string rawHtml = Encoding.UTF8.GetString(request.HtmlFileContent);
        string? rawCss = request.CssFileContent is not null
            ? Encoding.UTF8.GetString(request.CssFileContent)
            : null;

        ReportTemplate template = ReportTemplate.Create(
            name: request.Name,
            htmlContent: rawHtml,
            createdBy: userId,
            description: request.Description,
            templateCode: request.TemplateCode,
            cssContent: rawCss,
            paperSize: request.PaperSize,
            orientation: request.Orientation,
            widthPx: request.WidthPx,
            heightPx: request.HeightPx);

        _repository.Add(template);

        // Save once to get the Id committed before writing assets that reference it
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ── Save and register assets ──────────────────────────────────────
        var assetList = new List<TemplateAsset>();

        foreach ((string relativePath, byte[] bytes) in request.Assets)
        {
            string safeFileName = BuildSafeFileName(relativePath);
            string storagePath = await _storage.SaveAssetAsync(
                template.Id, safeFileName, bytes, cancellationToken);

            string sha256 = ComputeSha256(bytes);
            string contentType = DetectContentType(safeFileName);
            TemplateAssetType assetType = ClassifyAsset(safeFileName);

            TemplateAsset asset = template.AddAsset(
                assetType, safeFileName, contentType,
                relativePath, storagePath,
                bytes.Length, sha256);

            assetList.Add(asset);
        }

        // ── Rewrite asset references in HTML/CSS ──────────────────────────
        string sanitizedHtml = _sanitizer.Sanitize(rawHtml, request.AllowExternalAssets);
        string rewrittenHtml = _rewriter.RewriteHtml(sanitizedHtml, assetList);
        string? rewrittenCss = rawCss is not null
            ? _rewriter.RewriteCss(_sanitizer.Sanitize(rawCss, request.AllowExternalAssets), assetList)
            : null;

        template.UpdateContent(
            htmlContent: rewrittenHtml,
            cssContent: rewrittenCss,
            description: request.Description,
            templateCode: request.TemplateCode,
            paperSize: request.PaperSize,
            orientation: request.Orientation,
            widthPx: request.WidthPx,
            heightPx: request.HeightPx,
            modifiedBy: userId);

        // ── Take version snapshot ─────────────────────────────────────────
        string snapshotJson = JsonSerializer.Serialize(new
        {
            source = "html-import",
            originalFileName = request.HtmlFileName,
            importedAt = DateTimeOffset.UtcNow,
        });

        template.TakeSnapshot(userId, snapshotJson);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logImported(_logger, template.Id, template.Name, template.Version, null);

        return Result.Ok(new TemplateImportResultDto(
            Template: template.ToDto(),
            Assets: assetList.Select(a => a.ToDto()).ToList(),
            VersionNumber: template.Version,
            ImportedBy: userId,
            ImportedAt: DateTimeOffset.UtcNow));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildSafeFileName(string relativePath)
    {
        // Strip any directory component — keep only the file name
        string fileName = Path.GetFileName(relativePath);
        // Remove any remaining path separators / null bytes
        return string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
    }

    private static string ComputeSha256(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string DetectContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png"   => "image/png",
            ".jpg"   => "image/jpeg",
            ".jpeg"  => "image/jpeg",
            ".svg"   => "image/svg+xml",
            ".webp"  => "image/webp",
            ".ttf"   => "font/ttf",
            ".otf"   => "font/otf",
            ".woff"  => "font/woff",
            ".woff2" => "font/woff2",
            ".css"   => "text/css",
            ".html"  => "text/html",
            ".htm"   => "text/html",
            _        => "application/octet-stream",
        };

    private static TemplateAssetType ClassifyAsset(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".svg" or ".webp" => TemplateAssetType.Image,
            ".ttf" or ".otf" or ".woff" or ".woff2"          => TemplateAssetType.Font,
            ".css"                                            => TemplateAssetType.Stylesheet,
            _                                                 => TemplateAssetType.Other,
        };
}
