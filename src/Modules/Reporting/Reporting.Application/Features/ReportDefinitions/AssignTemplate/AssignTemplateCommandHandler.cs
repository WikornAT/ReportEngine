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
/// <para>
/// Associates a <see cref="ReportDefinition"/> with an Active template from the
/// Templates module, enabling HTML-based rendering for that report.
/// </para>
/// <para><b>Invariants enforced:</b>
/// <list type="bullet">
///   <item>Template must exist in the Templates module.</item>
///   <item>Template must be in <c>Active</c> status (Draft/Archived templates cannot be assigned).</item>
///   <item>Re-assigning the same template is a no-op (idempotent).</item>
///   <item>Report definition must not be Archived.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class AssignTemplateCommandHandler
    : IRequestHandler<AssignTemplateCommand, Result<ReportDefinitionDto>>
{
    // ── Structured log messages (zero-allocation via LoggerMessage) ───────────

    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logAssigned =
        LoggerMessage.Define<Guid, Guid, string>(
            LogLevel.Information,
            new EventId(1, "TemplateAssigned"),
            "Template {TemplateId} assigned to ReportDefinition {ReportDefinitionId} by {User}");

    private static readonly Action<ILogger, Guid, Guid, Guid, string, Exception?> _logReassigned =
        LoggerMessage.Define<Guid, Guid, Guid, string>(
            LogLevel.Warning,
            new EventId(2, "TemplateReassigned"),
            "ReportDefinition {ReportDefinitionId} template reassigned: {PreviousTemplateId} → {NewTemplateId} by {User}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> _logIdempotent =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Debug,
            new EventId(3, "TemplateAssignIdempotent"),
            "ReportDefinition {ReportDefinitionId} already assigned to template {TemplateId} — no-op");

    // ── Dependencies ──────────────────────────────────────────────────────────

    private readonly IReportingDbContext _dbContext;
    private readonly ITemplateVerifier _templateVerifier;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AssignTemplateCommandHandler> _logger;

    public AssignTemplateCommandHandler(
        IReportingDbContext dbContext,
        ITemplateVerifier templateVerifier,
        ICurrentUserService currentUser,
        ILogger<AssignTemplateCommandHandler> logger)
    {
        _dbContext = dbContext;
        _templateVerifier = templateVerifier;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReportDefinitionDto>> Handle(
        AssignTemplateCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Load report definition ─────────────────────────────────────────
        ReportDefinition? definition = await _dbContext.ReportDefinitions
            .Include(d => d.Parameters)
            .Include(d => d.DataSources)
            .FirstOrDefaultAsync(d => d.Id == request.ReportDefinitionId, cancellationToken);

        if (definition is null)
        {
            return AppError.NotFound(nameof(ReportDefinition), request.ReportDefinitionId);
        }

        // ── 2. Idempotency guard ──────────────────────────────────────────────
        if (definition.TemplateId == request.TemplateId)
        {
            _logIdempotent(_logger, definition.Id, request.TemplateId, null);
            return definition.ToDto();
        }

        // ── 3. Validate template exists ───────────────────────────────────────
        bool templateExists = await _templateVerifier.ExistsAsync(
            request.TemplateId, cancellationToken);

        if (!templateExists)
        {
            return AppError.NotFound("ReportTemplate", request.TemplateId);
        }

        // ── 4. Validate template is Active ────────────────────────────────────
        bool templateActive = await _templateVerifier.IsActiveAsync(
            request.TemplateId, cancellationToken);

        if (!templateActive)
        {
            return AppError.DomainViolation(
                $"Template '{request.TemplateId}' is not Active. " +
                "Only Active templates can be assigned to a report definition.");
        }

        // ── 5. Apply domain change ────────────────────────────────────────────
        Guid? previousTemplateId = definition.TemplateId;

        try
        {
            definition.AssignTemplate(
                templateId: request.TemplateId,
                templatePath: $"html-template/{request.TemplateId}",
                modifiedBy: _currentUser.UserId);
        }
        catch (ReportingDomainException ex)
        {
            return AppError.DomainViolation(ex.Message);
        }

        // ── 6. Persist ────────────────────────────────────────────────────────
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ── 7. Audit log ──────────────────────────────────────────────────────
        if (previousTemplateId.HasValue)
        {
            _logReassigned(_logger,
                definition.Id,
                previousTemplateId.Value,
                request.TemplateId,
                _currentUser.UserId,
                null);
        }
        else
        {
            _logAssigned(_logger, request.TemplateId, definition.Id, _currentUser.UserId, null);
        }

        return definition.ToDto();
    }
}

