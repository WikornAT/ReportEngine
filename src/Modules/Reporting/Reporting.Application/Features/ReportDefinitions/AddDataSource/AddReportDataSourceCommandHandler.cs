using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.AddDataSource;

/// <summary>
/// Handles <see cref="AddReportDataSourceCommand"/>.
/// Delegates to <see cref="ReportDefinition.AddDataSource"/> and persists.
/// </summary>
internal sealed class AddReportDataSourceCommandHandler
    : IRequestHandler<AddReportDataSourceCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> _logDataSourceAdded =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDataSourceAdded"),
            "DataSource '{Name}' added to ReportDefinition {Id} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AddReportDataSourceCommandHandler> _logger;

    public AddReportDataSourceCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<AddReportDataSourceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        AddReportDataSourceCommand request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .Include(r => r.Parameters)
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        try
        {
            definition.AddDataSource(
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

        _logDataSourceAdded(_logger, request.Name, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
