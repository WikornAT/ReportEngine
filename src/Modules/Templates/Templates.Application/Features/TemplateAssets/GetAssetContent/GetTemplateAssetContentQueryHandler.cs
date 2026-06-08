using MediatR;

using Microsoft.EntityFrameworkCore;

using ReportEngine.SharedKernel;

using Templates.Application.Contracts;
using Templates.Domain.ReportTemplates;

namespace Templates.Application.Features.TemplateAssets.GetAssetContent;

internal sealed class GetTemplateAssetContentQueryHandler
    : IRequestHandler<GetTemplateAssetContentQuery, Result<AssetContentResult>>
{
    private readonly ITemplatesDbContext _dbContext;
    private readonly ITemplateStorageService _storage;

    public GetTemplateAssetContentQueryHandler(
        ITemplatesDbContext dbContext,
        ITemplateStorageService storage)
    {
        _dbContext = dbContext;
        _storage = storage;
    }

    public async Task<Result<AssetContentResult>> Handle(
        GetTemplateAssetContentQuery request,
        CancellationToken cancellationToken)
    {
        TemplateAsset? asset = await _dbContext.TemplateAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == request.AssetId && a.TemplateId == request.TemplateId,
                cancellationToken);

        if (asset is null)
        {
            return AppError.NotFound(nameof(TemplateAsset), request.AssetId);
        }

        byte[] content = await _storage.ReadAssetAsync(asset.StoragePath, cancellationToken);

        return Result.Ok(new AssetContentResult(content, asset.ContentType, asset.FileName));
    }
}
