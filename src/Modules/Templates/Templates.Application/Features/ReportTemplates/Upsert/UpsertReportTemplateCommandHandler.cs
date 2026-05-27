using MediatR;

using Exim.ReportEngine.SharedKernel;
using Templates.Application.Contracts;
using Templates.Application.DTOs;
using Templates.Application.Mapping;
using Templates.Domain.ReportTemplates;

namespace Templates.Application.Features.ReportTemplates.Upsert;

internal sealed class UpsertReportTemplateCommandHandler
    : IRequestHandler<UpsertReportTemplateCommand, Result<ReportTemplateDto>>
{
    private readonly IReportTemplateRepository _repository;
    private readonly ITemplatesDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public UpsertReportTemplateCommandHandler(
        IReportTemplateRepository repository,
        ITemplatesDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ReportTemplateDto>> Handle(
        UpsertReportTemplateCommand request,
        CancellationToken cancellationToken)
    {
        ReportTemplate template;

        if (request.Id.HasValue)
        {
            // ── Update existing ───────────────────────────────────────────────
            ReportTemplate? existing = await _repository.GetByIdAsync(request.Id.Value, cancellationToken);

            if (existing is null)
            {
                return AppError.NotFound(nameof(ReportTemplate), request.Id.Value);
            }

            existing.UpdateContent(
                htmlContent: request.HtmlContent,
                cssContent: request.CssContent,
                description: request.Description,
                paperSize: request.PaperSize,
                orientation: request.Orientation,
                widthPx: request.WidthPx,
                heightPx: request.HeightPx,
                modifiedBy: _currentUser.UserId);

            template = existing;
        }
        else
        {
            // ── Create new ────────────────────────────────────────────────────
            template = ReportTemplate.Create(
                name: request.Name,
                htmlContent: request.HtmlContent,
                createdBy: _currentUser.UserId,
                description: request.Description,
                cssContent: request.CssContent,
                paperSize: request.PaperSize,
                orientation: request.Orientation,
                widthPx: request.WidthPx,
                heightPx: request.HeightPx);

            _repository.Add(template);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return template.ToDto();
    }
}
