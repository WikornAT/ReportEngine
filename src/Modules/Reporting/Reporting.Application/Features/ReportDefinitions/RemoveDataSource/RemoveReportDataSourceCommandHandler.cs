using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.RemoveDataSource;

/// <summary>
/// Handles <see cref="RemoveReportDataSourceCommand"/>.
/// Delegates to <see cref="ReportDefinition.RemoveDataSource"/> and persists.
/// </summary>
internal sealed class RemoveReportDataSourceCommandHandler
    : IRequestHandler<RemoveReportDataSourceCommand, Result<Unit>>
{
    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logRemoved =
        LoggerMessage.Define<Guid, Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDataSourceRemoved"),
            "DataSource {DataSourceId} removed from ReportDefinition {ReportDefinitionId} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RemoveReportDataSourceCommandHandler> _logger;

    public RemoveReportDataSourceCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<RemoveReportDataSourceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        RemoveReportDataSourceCommand request,
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
            definition.RemoveDataSource(
                dataSourceId: request.DataSourceId,
                modifiedBy: _currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logRemoved(_logger, request.DataSourceId, definition.Id, _currentUser.UserId, null);

        return Unit.Value;
    }
}
