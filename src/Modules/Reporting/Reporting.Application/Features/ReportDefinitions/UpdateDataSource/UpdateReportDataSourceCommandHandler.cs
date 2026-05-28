using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.UpdateDataSource;

/// <summary>
/// Handles <see cref="UpdateReportDataSourceCommand"/>.
/// Delegates to <see cref="ReportDefinition.UpdateDataSource"/> and persists.
/// </summary>
internal sealed class UpdateReportDataSourceCommandHandler
    : IRequestHandler<UpdateReportDataSourceCommand, Result<ReportDataSourceDto>>
{
    private static readonly Action<ILogger, string, Guid, Guid, string, Exception?> _logUpdated =
        LoggerMessage.Define<string, Guid, Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDataSourceUpdated"),
            "DataSource '{Name}' (id {DataSourceId}) on ReportDefinition {ReportDefinitionId} updated by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateReportDataSourceCommandHandler> _logger;

    public UpdateReportDataSourceCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<UpdateReportDataSourceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDataSourceDto>> Handle(
        UpdateReportDataSourceCommand request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        if (!definition.DataSources.Any(ds => ds.Id == request.DataSourceId))
        {
            return AppError.NotFound(nameof(ReportDataSource), request.DataSourceId);
        }

        try
        {
            definition.UpdateDataSource(
                dataSourceId: request.DataSourceId,
                name: request.Name,
                dataSourceType: request.DataSourceType,
                connectionStringName: request.ConnectionStringName,
                queryText: request.QueryText,
                sortOrder: request.SortOrder,
                modifiedBy: _currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        ReportDataSource updated = definition.DataSources.First(ds => ds.Id == request.DataSourceId);

        _logUpdated(_logger, updated.Name, updated.Id, definition.Id, _currentUser.UserId, null);

        return updated.ToDto();
    }
}
