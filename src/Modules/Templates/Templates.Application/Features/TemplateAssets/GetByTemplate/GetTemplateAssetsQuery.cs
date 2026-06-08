using MediatR;

using Microsoft.EntityFrameworkCore;

using ReportEngine.SharedKernel;

using Templates.Application.Contracts;
using Templates.Application.DTOs;
using Templates.Application.Mapping;
using Templates.Domain.ReportTemplates;

namespace Templates.Application.Features.TemplateAssets.GetByTemplate;

public sealed record GetTemplateAssetsQuery(Guid TemplateId) : IRequest<Result<IReadOnlyList<TemplateAssetDto>>>;

internal sealed class GetTemplateAssetsQueryHandler
    : IRequestHandler<GetTemplateAssetsQuery, Result<IReadOnlyList<TemplateAssetDto>>>
{
    private readonly ITemplatesDbContext _dbContext;

    public GetTemplateAssetsQueryHandler(ITemplatesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<TemplateAssetDto>>> Handle(
        GetTemplateAssetsQuery request,
        CancellationToken cancellationToken)
    {
        bool templateExists = await _dbContext.ReportTemplates
            .AnyAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (!templateExists)
        {
            return AppError.NotFound(nameof(ReportTemplate), request.TemplateId);
        }

        IReadOnlyList<TemplateAssetDto> assets = await _dbContext.TemplateAssets
            .AsNoTracking()
            .Where(a => a.TemplateId == request.TemplateId)
            .OrderBy(a => a.FileName)
            .Select(a => a.ToDto())
            .ToListAsync(cancellationToken);

        return Result.Ok(assets);
    }
}
