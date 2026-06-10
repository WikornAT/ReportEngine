using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.RemoveDataSourceParameter;

/// <summary>Handles <see cref="RemoveDataSourceParameterCommand"/>.</summary>
internal sealed class RemoveDataSourceParameterCommandHandler
    : IRequestHandler<RemoveDataSourceParameterCommand, Result<Unit>>
{
    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logRemoved =
        LoggerMessage.Define<Guid, Guid, string>(
            LogLevel.Information,
            new EventId(1, "DataSourceParameterRemoved"),
            "Parameter {ParameterId} removed from DataSource {DataSourceId} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RemoveDataSourceParameterCommandHandler> _logger;

    public RemoveDataSourceParameterCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<RemoveDataSourceParameterCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        RemoveDataSourceParameterCommand request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .Include(r => r.DataSources)
                .ThenInclude(ds => ds.Parameters)
            .FirstOrDefaultAsync(r => r.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        try
        {
            definition.RemoveDataSourceParameter(
                dataSourceId: request.DataSourceId,
                parameterId: request.ParameterId,
                modifiedBy: _currentUser.UserId);
        }
        catch (Exception ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logRemoved(_logger, request.ParameterId, request.DataSourceId,
            _currentUser.UserId, null);

        return Unit.Value;
    }
}
