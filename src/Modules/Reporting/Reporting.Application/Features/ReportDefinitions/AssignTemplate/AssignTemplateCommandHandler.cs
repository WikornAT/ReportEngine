using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.Common;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.AssignTemplate;

/// <summary>
/// Handles <see cref="AssignTemplateCommand"/>.
/// Associates a <see cref="ReportDefinition"/> with a template from the Templates module.
/// </summary>
internal sealed class AssignTemplateCommandHandler
    : IRequestHandler<AssignTemplateCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logAssigned =
        LoggerMessage.Define<Guid, Guid, string>(
            LogLevel.Information,
            new EventId(1, "TemplateAssigned"),
            "Template {TemplateId} assigned to ReportDefinition {ReportDefinitionId} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AssignTemplateCommandHandler> _logger;

    public AssignTemplateCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<AssignTemplateCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        AssignTemplateCommand request,
        CancellationToken cancellationToken)
    {
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .Include(d => d.Parameters)
            .Include(d => d.DataSources)
            .FirstOrDefaultAsync(d => d.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        try
        {
            definition.AssignTemplate(
                templateId: request.TemplateId,
                templatePath: request.TemplatePath,
                modifiedBy: _currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logAssigned(_logger, request.TemplateId, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
