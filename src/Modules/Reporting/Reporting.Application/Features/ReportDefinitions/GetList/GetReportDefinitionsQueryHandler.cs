using MediatR;

using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.GetList;

/// <summary>
/// Handles <see cref="GetReportDefinitionsQuery"/>.
/// Applies optional filters server-side and returns a paged result.
/// Children (Parameters / DataSources) are NOT included in list results for performance;
/// use <c>GetReportDefinitionByIdQuery</c> for the full aggregate.
/// </summary>
internal sealed class GetReportDefinitionsQueryHandler
    : IRequestHandler<GetReportDefinitionsQuery, Result<PagedResult<ReportDefinitionDto>>>
{
    private readonly IReportingDbContext _dbContext;

    public GetReportDefinitionsQueryHandler(IReportingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<ReportDefinitionDto>>> Handle(
        GetReportDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<ReportDefinition> query = _dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Parameters)
            .Include(r => r.DataSources);

        // ── Filters ───────────────────────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(r => r.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string pattern = $"%{request.SearchTerm}%";
            query = query.Where(r => EF.Functions.Like(r.Name, pattern));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (!request.IncludeHidden)
        {
            query = query.Where(r => !r.IsHidden);
        }

        // ── Paging ────────────────────────────────────────────────────────────

        int totalCount = await query.CountAsync(cancellationToken);

        List<ReportDefinition> items = await query
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportDefinitionDto>(
            Items: items.Select(r => r.ToDto()).ToList(),
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize);
    }
}
