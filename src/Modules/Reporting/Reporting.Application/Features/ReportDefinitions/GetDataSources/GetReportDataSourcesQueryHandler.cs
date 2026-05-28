using MediatR;

using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.GetDataSources;

/// <summary>
/// Handles <see cref="GetReportDataSourcesQuery"/>.
/// Returns all data sources for a report definition, ordered by <c>SortOrder</c>.
/// </summary>
internal sealed class GetReportDataSourcesQueryHandler
    : IRequestHandler<GetReportDataSourcesQuery, Result<IReadOnlyList<ReportDataSourceDto>>>
{
    private readonly IReportingDbContext _dbContext;

    public GetReportDataSourcesQueryHandler(IReportingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ReportDataSourceDto>>> Handle(
        GetReportDataSourcesQuery request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        IReadOnlyList<ReportDataSourceDto> dataSources = definition.DataSources
            .OrderBy(ds => ds.SortOrder)
            .Select(ds => ds.ToDto())
            .ToList();

        return Result.Ok(dataSources);
    }
}
