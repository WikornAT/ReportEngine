using Templates.Domain.Enums;

namespace Templates.Domain.ReportTemplates;

/// <summary>
/// Represents a binary asset (image, font, stylesheet) that belongs to a
/// <see cref="ReportTemplate"/> and is referenced from its HTML or CSS content.
/// <para>
/// Assets are addressed via a secure API endpoint —
/// <c>/api/v1/templates/{templateId}/assets/{assetId}/content</c> —
/// so that no physical file path is ever exposed to clients.
/// </para>
/// </summary>
public sealed class TemplateAsset
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>FK to the owning <see cref="ReportTemplate"/>.</summary>
    public Guid TemplateId { get; private set; }

    // ── Classification ────────────────────────────────────────────────────────

    /// <summary>Content category of this asset.</summary>
    public TemplateAssetType AssetType { get; private set; }

    // ── File metadata ─────────────────────────────────────────────────────────

    /// <summary>Original file name as uploaded (e.g., <c>logo.png</c>).</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>MIME content type detected at import time (e.g., <c>image/png</c>).</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// Relative path within the template package (e.g., <c>assets/logo.png</c>).
    /// Used to match references inside the HTML/CSS during the rewrite phase.
    /// </summary>
    public string RelativePath { get; private set; } = string.Empty;

    /// <summary>
    /// Absolute path on the server file system where the asset bytes are stored.
    /// <b>Never exposed to clients.</b>
    /// </summary>
    public string StoragePath { get; private set; } = string.Empty;

    /// <summary>
    /// Public URL served to browsers at render time.
    /// Format: <c>/api/templates/report-templates/{templateId}/assets/{id}/content</c>.
    /// </summary>
    public string PublicUrl { get; private set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; private set; }

    /// <summary>SHA-256 hex digest of the file content for integrity checking.</summary>
    public string Sha256Hash { get; private set; } = string.Empty;

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAt { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    private TemplateAsset() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates a new <see cref="TemplateAsset"/> record.</summary>
    internal static TemplateAsset Create(
        Guid id,
        Guid templateId,
        TemplateAssetType assetType,
        string fileName,
        string contentType,
        string relativePath,
        string storagePath,
        string publicUrl,
        long sizeBytes,
        string sha256Hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256Hash);

        return new TemplateAsset
        {
            Id = id,
            TemplateId = templateId,
            AssetType = assetType,
            FileName = fileName,
            ContentType = contentType,
            RelativePath = relativePath,
            StoragePath = storagePath,
            PublicUrl = publicUrl,
            SizeBytes = sizeBytes,
            Sha256Hash = sha256Hash,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
