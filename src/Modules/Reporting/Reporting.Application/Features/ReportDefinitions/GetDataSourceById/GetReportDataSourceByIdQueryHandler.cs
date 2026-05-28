using MediatR;

using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.GetDataSourceById;

/// <summary>
/// Handles <see cref="GetReportDataSourceByIdQuery"/>.
/// Loads the parent aggregate and projects the requested data source.
/// </summary>
internal sealed class GetReportDataSourceByIdQueryHandler
    : IRequestHandler<GetReportDataSourceByIdQuery, Result<ReportDataSourceDto>>
{
    private readonly IReportingDbContext _dbContext;

    public GetReportDataSourceByIdQueryHandler(IReportingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ReportDataSourceDto>> Handle(
        GetReportDataSourceByIdQuery request,
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

        ReportDataSource? dataSource = definition.DataSources
            .FirstOrDefault(ds => ds.Id == request.DataSourceId);

        if (dataSource is null)
        {
            return AppError.NotFound(nameof(ReportDataSource), request.DataSourceId);
        }

        return dataSource.ToDto();
    }
}
