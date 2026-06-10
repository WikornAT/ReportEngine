using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using ReportEngine.SharedKernel;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.AddDataSourceParameter;

/// <summary>Handles <see cref="AddDataSourceParameterCommand"/>.</summary>
internal sealed class AddDataSourceParameterCommandHandler
    : IRequestHandler<AddDataSourceParameterCommand, Result<ReportDataSourceDto>>
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> _logAdded =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(1, "DataSourceParameterAdded"),
            "Parameter '{SourceName}' added to DataSource {DataSourceId} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AddDataSourceParameterCommandHandler> _logger;

    public AddDataSourceParameterCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<AddDataSourceParameterCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDataSourceDto>> Handle(
        AddDataSourceParameterCommand request,
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
            definition.AddDataSourceParameter(
                dataSourceId: request.DataSourceId,
                sourceParameterName: request.SourceParameterName,
                reportParameterName: request.ReportParameterName,
                dbType: request.DbType,
                isRequired: request.IsRequired,
                defaultValue: request.DefaultValue,
                modifiedBy: _currentUser.UserId);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logAdded(_logger, request.SourceParameterName, request.DataSourceId,
                _currentUser.UserId, null);

            return definition.DataSources.First(ds => ds.Id == request.DataSourceId).ToDto();
        }
        catch (Exception ex)
        {
            return AppError.DomainViolation(ex.Message);
        }
    }
}
