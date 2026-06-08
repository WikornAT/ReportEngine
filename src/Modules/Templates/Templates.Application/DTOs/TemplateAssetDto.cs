using Templates.Domain.Enums;

namespace Templates.Application.DTOs;

/// <summary>Read-model DTO for a <see cref="Domain.ReportTemplates.TemplateAsset"/>.</summary>
public sealed record TemplateAssetDto(
    Guid Id,
    Guid TemplateId,
    TemplateAssetType AssetType,
    string FileName,
    string ContentType,
    string RelativePath,
    string PublicUrl,
    long SizeBytes,
    string Sha256Hash,
    DateTimeOffset CreatedAt);
