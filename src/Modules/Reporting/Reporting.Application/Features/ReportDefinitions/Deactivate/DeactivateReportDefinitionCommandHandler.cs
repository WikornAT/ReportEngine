using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.Deactivate;

/// <summary>
/// Handles <see cref="DeactivateReportDefinitionCommand"/>.
/// Calls <see cref="ReportDefinition.Deactivate"/> and persists the state change.
/// </summary>
internal sealed class DeactivateReportDefinitionCommandHandler
    : IRequestHandler<DeactivateReportDefinitionCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, Guid, string, Exception?> _logDeactivated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDefinitionDeactivated"),
            "ReportDefinition {Id} deactivated by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeactivateReportDefinitionCommandHandler> _logger;

    public DeactivateReportDefinitionCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<DeactivateReportDefinitionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        DeactivateReportDefinitionCommand request,
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
            definition.Deactivate(_currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logDeactivated(_logger, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
