using MediatR;

using ReportEngine.SharedKernel;

namespace Templates.Application.Features.TemplateAssets.GetAssetContent;

/// <summary>
/// Returns the raw bytes and content type for a single template asset, for serving via HTTP.
/// </summary>
public sealed record GetTemplateAssetContentQuery(
    Guid TemplateId,
    Guid AssetId
) : IRequest<Result<AssetContentResult>>;

/// <summary>The binary content and MIME type of an asset file.</summary>
public sealed record AssetContentResult(
    byte[] Content,
    string ContentType,
    string FileName);
