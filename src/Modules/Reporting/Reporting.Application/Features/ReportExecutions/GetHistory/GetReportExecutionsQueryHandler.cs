using MediatR;

using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Application.Features.ReportDefinitions.GetList;
using Reporting.Domain.ReportExecutions;

namespace Reporting.Application.Features.ReportExecutions.GetHistory;

/// <summary>
/// Handles <see cref="GetReportExecutionsQuery"/>.
/// Returns paged execution history with output file details included.
/// </summary>
internal sealed class GetReportExecutionsQueryHandler
    : IRequestHandler<GetReportExecutionsQuery, Result<PagedResult<ReportExecutionDto>>>
{
    private readonly IReportingDbContext _dbContext;

    public GetReportExecutionsQueryHandler(IReportingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<ReportExecutionDto>>> Handle(
        GetReportExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<ReportExecution> query = _dbContext.ReportExecutions
            .AsNoTracking()
            .Include(e => e.OutputFiles);

        // ── Filters ───────────────────────────────────────────────────────────

        if (request.ReportDefinitionId.HasValue)
        {
            query = query.Where(e => e.ReportDefinitionId == request.ReportDefinitionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TriggeredBy))
        {
            query = query.Where(e => e.TriggeredBy == request.TriggeredBy);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        // ── Paging ────────────────────────────────────────────────────────────

        int totalCount = await query.CountAsync(cancellationToken);

        List<ReportExecution> items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportExecutionDto>(
            Items: items.Select(e => e.ToDto()).ToList(),
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize);
    }
}
