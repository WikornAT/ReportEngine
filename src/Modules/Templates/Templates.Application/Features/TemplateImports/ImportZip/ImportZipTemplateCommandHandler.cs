using System.IO.Compression;
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

namespace Templates.Application.Features.TemplateImports.ImportZip;

internal sealed class ImportZipTemplateCommandHandler
    : IRequestHandler<ImportZipTemplateCommand, Result<TemplateImportResultDto>>
{
    private const long _maxUncompressedBytes = 50 * 1024 * 1024; // 50 MB

    private static readonly HashSet<string> _allowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".css",
            ".png", ".jpg", ".jpeg", ".svg", ".webp",
            ".ttf", ".otf", ".woff", ".woff2",
        };

    private static readonly HashSet<string> _blockedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".js",
        };

    private static readonly Action<ILogger, string, Exception?> _logImporting =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(110, "ZipTemplateImporting"),
            "Importing ZIP template '{Name}'");

    private static readonly Action<ILogger, Guid, string, int, Exception?> _logImported =
        LoggerMessage.Define<Guid, string, int>(
            LogLevel.Information,
            new EventId(111, "ZipTemplateImported"),
            "ZIP template '{Name}' imported as {Id} (v{Version})");

    private readonly ITemplatesDbContext _dbContext;
    private readonly IReportTemplateRepository _repository;
    private readonly ITemplateStorageService _storage;
    private readonly ITemplateHtmlSanitizer _sanitizer;
    private readonly ITemplateAssetReferenceRewriter _rewriter;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ImportZipTemplateCommandHandler> _logger;

    public ImportZipTemplateCommandHandler(
        ITemplatesDbContext dbContext,
        IReportTemplateRepository repository,
        ITemplateStorageService storage,
        ITemplateHtmlSanitizer sanitizer,
        ITemplateAssetReferenceRewriter rewriter,
        ICurrentUserService currentUser,
        ILogger<ImportZipTemplateCommandHandler> logger)
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
        ImportZipTemplateCommand request,
        CancellationToken cancellationToken)
    {
        // ── Validate ZIP extension ────────────────────────────────────────
        if (!Path.GetExtension(request.ZipFileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return AppError.Validation("zipFile must be a .zip file.");
        }

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

        // ── Extract and validate ZIP entries ──────────────────────────────
        Dictionary<string, byte[]> extractedFiles;
        try
        {
            extractedFiles = ExtractAndValidate(request.ZipFileContent);
        }
        catch (InvalidOperationException ex)
        {
            return AppError.Validation(ex.Message);
        }

        // ── Locate required HTML file ─────────────────────────────────────
        string? htmlKey = extractedFiles.Keys
            .FirstOrDefault(k => k.Equals("template.html", StringComparison.OrdinalIgnoreCase));

        if (htmlKey is null)
        {
            return AppError.Validation(
                "ZIP archive must contain a file named 'template.html' at the root.");
        }

        string? cssKey = extractedFiles.Keys
            .FirstOrDefault(k => k.Equals("style.css", StringComparison.OrdinalIgnoreCase));

        string rawHtml = Encoding.UTF8.GetString(extractedFiles[htmlKey]);
        string? rawCss = cssKey is not null
            ? Encoding.UTF8.GetString(extractedFiles[cssKey])
            : null;

        _logImporting(_logger, request.Name, null);
        string userId = _currentUser.UserId;

        // ── Create template aggregate ─────────────────────────────────────
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ── Save and register asset files ─────────────────────────────────
        var assetList = new List<TemplateAsset>();

        foreach ((string relativePath, byte[] bytes) in extractedFiles)
        {
            // Skip the main HTML and CSS — they are stored in the template content fields
            if (relativePath.Equals(htmlKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (cssKey is not null &&
                relativePath.Equals(cssKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string safeFileName = BuildSafeFileName(relativePath);
            string storagePath = await _storage.SaveAssetAsync(
                template.Id, safeFileName, bytes, cancellationToken);

            string sha256 = ComputeSha256(bytes);
            string contentType = DetectContentType(safeFileName);
            TemplateAssetType assetType = ClassifyAsset(safeFileName);

            TemplateAsset asset = template.AddAsset(
                assetType, safeFileName, contentType,
                relativePath, storagePath, bytes.Length, sha256);

            assetList.Add(asset);
        }

        // ── Sanitize and rewrite HTML/CSS ─────────────────────────────────
        string sanitizedHtml = _sanitizer.Sanitize(rawHtml, request.AllowExternalAssets);
        string rewrittenHtml = _rewriter.RewriteHtml(sanitizedHtml, assetList);
        string? rewrittenCss = rawCss is not null
            ? _rewriter.RewriteCss(
                _sanitizer.Sanitize(rawCss, request.AllowExternalAssets), assetList)
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

        // ── Version snapshot ──────────────────────────────────────────────
        string snapshotJson = JsonSerializer.Serialize(new
        {
            source = "zip-import",
            originalFileName = request.ZipFileName,
            entryCount = extractedFiles.Count,
            assetCount = assetList.Count,
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

    // ── ZIP extraction ────────────────────────────────────────────────────────

    private static Dictionary<string, byte[]> ExtractAndValidate(byte[] zipBytes)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        long totalUncompressed = 0;

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // Skip directories
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            // Zip Slip guard — normalize and confirm the path stays within the archive root
            string normalizedPath = NormalizePath(entry.FullName);
            if (normalizedPath.StartsWith("..", StringComparison.Ordinal) ||
                Path.IsPathRooted(normalizedPath))
            {
                throw new InvalidOperationException(
                    $"ZIP entry '{entry.FullName}' contains a path traversal sequence.");
            }

            string ext = Path.GetExtension(entry.Name);

            if (_blockedExtensions.Contains(ext))
            {
                throw new InvalidOperationException(
                    $"ZIP entry '{entry.FullName}' has a blocked extension '{ext}'.");
            }

            if (!_allowedExtensions.Contains(ext))
            {
                throw new InvalidOperationException(
                    $"ZIP entry '{entry.FullName}' has an unsupported extension '{ext}'.");
            }

            totalUncompressed += entry.Length;
            if (totalUncompressed > _maxUncompressedBytes)
            {
                throw new InvalidOperationException(
                    $"ZIP total uncompressed size exceeds the {_maxUncompressedBytes / (1024 * 1024)} MB limit.");
            }

            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            result[normalizedPath] = ms.ToArray();
        }

        return result;
    }

    private static string NormalizePath(string rawPath)
    {
        // Replace backslashes, collapse ./ and trim leading slashes
        string path = rawPath.Replace('\\', '/');
        path = path.TrimStart('/');
        return path;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildSafeFileName(string relativePath)
    {
        string fileName = Path.GetFileName(relativePath);
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
