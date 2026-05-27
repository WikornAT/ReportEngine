using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.Update;

/// <summary>
/// Handles <see cref="UpdateReportDefinitionCommand"/>.
/// Updates metadata on an existing <see cref="ReportDefinition"/> aggregate.
/// </summary>
internal sealed class UpdateReportDefinitionCommandHandler
    : IRequestHandler<UpdateReportDefinitionCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, Guid, string, Exception?> _logUpdated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDefinitionUpdated"),
            "ReportDefinition {Id} updated by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateReportDefinitionCommandHandler> _logger;

    public UpdateReportDefinitionCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<UpdateReportDefinitionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        UpdateReportDefinitionCommand request,
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
            definition.UpdateMetadata(
                name: request.Name,
                description: request.Description,
                category: request.Category,
                subCategory: request.SubCategory,
                modifiedBy: _currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logUpdated(_logger, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
