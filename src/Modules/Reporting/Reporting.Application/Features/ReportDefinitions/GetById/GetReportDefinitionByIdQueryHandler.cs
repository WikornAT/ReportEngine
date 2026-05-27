using MediatR;

using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.GetById;

/// <summary>
/// Handles <see cref="GetReportDefinitionByIdQuery"/>.
/// Loads the aggregate with all children from the read side.
/// </summary>
internal sealed class GetReportDefinitionByIdQueryHandler
    : IRequestHandler<GetReportDefinitionByIdQuery, Result<ReportDefinitionDto>>
{
    private readonly IReportingDbContext _dbContext;

    public GetReportDefinitionByIdQueryHandler(IReportingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        GetReportDefinitionByIdQuery request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Parameters)
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.Id);
        }

        return definition.ToDto();
    }
}
