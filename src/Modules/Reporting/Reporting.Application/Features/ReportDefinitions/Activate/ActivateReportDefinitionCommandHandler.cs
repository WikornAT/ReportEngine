using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.Activate;

/// <summary>
/// Handles <see cref="ActivateReportDefinitionCommand"/>.
/// Calls <see cref="ReportDefinition.Publish"/> and persists the state change.
/// </summary>
internal sealed class ActivateReportDefinitionCommandHandler
    : IRequestHandler<ActivateReportDefinitionCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, Guid, string, Exception?> _logActivated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDefinitionActivated"),
            "ReportDefinition {Id} activated by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ActivateReportDefinitionCommandHandler> _logger;

    public ActivateReportDefinitionCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<ActivateReportDefinitionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        ActivateReportDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .Include(r => r.Parameters)
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.Id);
        }

        try
        {
            definition.Publish(_currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logActivated(_logger, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
