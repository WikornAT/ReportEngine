using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using ReportEngine.SharedKernel;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.UpdateDataSourceParameter;

/// <summary>Handles <see cref="UpdateDataSourceParameterCommand"/>.</summary>
internal sealed class UpdateDataSourceParameterCommandHandler
    : IRequestHandler<UpdateDataSourceParameterCommand, Result<ReportDataSourceDto>>
{
    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logUpdated =
        LoggerMessage.Define<Guid, Guid, string>(
            LogLevel.Information,
            new EventId(1, "DataSourceParameterUpdated"),
            "Parameter {ParameterId} on DataSource {DataSourceId} updated by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateDataSourceParameterCommandHandler> _logger;

    public UpdateDataSourceParameterCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<UpdateDataSourceParameterCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDataSourceDto>> Handle(
        UpdateDataSourceParameterCommand request,
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
            definition.UpdateDataSourceParameter(
                dataSourceId: request.DataSourceId,
                parameterId: request.ParameterId,
                sourceParameterName: request.SourceParameterName,
                reportParameterName: request.ReportParameterName,
                dbType: request.DbType,
                isRequired: request.IsRequired,
                defaultValue: request.DefaultValue,
                modifiedBy: _currentUser.UserId);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logUpdated(_logger, request.ParameterId, request.DataSourceId,
                _currentUser.UserId, null);

            return definition.DataSources.First(ds => ds.Id == request.DataSourceId).ToDto();
        }
        catch (Exception ex)
        {
            return AppError.DomainViolation(ex.Message);
        }
    }
}
