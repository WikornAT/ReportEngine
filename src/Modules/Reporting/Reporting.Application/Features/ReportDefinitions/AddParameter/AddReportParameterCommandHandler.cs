using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.AddParameter;

/// <summary>
/// Handles <see cref="AddReportParameterCommand"/>.
/// Delegates to <see cref="ReportDefinition.AddParameter"/> and persists.
/// </summary>
internal sealed class AddReportParameterCommandHandler
    : IRequestHandler<AddReportParameterCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> _logParameterAdded =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportParameterAdded"),
            "Parameter '{Name}' added to ReportDefinition {Id} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AddReportParameterCommandHandler> _logger;

    public AddReportParameterCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<AddReportParameterCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        AddReportParameterCommand request,
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
            definition.AddParameter(
                name: request.Name,
                displayName: request.DisplayName,
                parameterType: request.ParameterType,
                isRequired: request.IsRequired,
                defaultValue: request.DefaultValue,
                sortOrder: request.SortOrder,
                isVisible: request.IsVisible,
                modifiedBy: _currentUser.UserId,
                description: request.Description);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logParameterAdded(_logger, request.Name, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
