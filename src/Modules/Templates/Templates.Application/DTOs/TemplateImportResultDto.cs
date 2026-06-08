namespace Templates.Application.DTOs;

/// <summary>
/// Returned from a successful template import operation.
/// Carries the full template descriptor plus a list of registered assets.
/// </summary>
public sealed record TemplateImportResultDto(
    ReportTemplateDto Template,
    IReadOnlyList<TemplateAssetDto> Assets,
    int VersionNumber,
    string ImportedBy,
    DateTimeOffset ImportedAt);
