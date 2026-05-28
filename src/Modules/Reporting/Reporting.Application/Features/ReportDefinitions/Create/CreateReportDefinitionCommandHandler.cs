using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Reporting.Application.Contracts;
using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Mapping;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Features.ReportDefinitions.Create;

/// <summary>
/// Handles <see cref="CreateReportDefinitionCommand"/>.
/// Creates a new <see cref="ReportDefinition"/> aggregate and persists it.
/// </summary>
internal sealed class CreateReportDefinitionCommandHandler
    : IRequestHandler<CreateReportDefinitionCommand, Result<ReportDefinitionDto>>
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> _logCreated =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(1, "ReportDefinitionCreated"),
            "ReportDefinition '{Name}' created with id {Id} by {User}");

    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateReportDefinitionCommandHandler> _logger;

    public CreateReportDefinitionCommandHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<CreateReportDefinitionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        CreateReportDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        bool exists = await _dbContext.ReportDefinitions
            .AnyAsync(
                r => r.Name == request.Name && r.Category == request.Category,
                cancellationToken);

        if (exists)
        {
            return AppError.Conflict(
                $"A report named '{request.Name}' already exists in category '{request.Category}'.");
        }

        ReportDefinition definition = ReportDefinition.Create(
            name: request.Name,
            category: request.Category,
            createdBy: _currentUser.UserId,
            description: request.Description,
            subCategory: request.SubCategory);

        _dbContext.ReportDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logCreated(_logger, definition.Name, definition.Id, _currentUser.UserId, null);

        return definition.ToDto();
    }
}
