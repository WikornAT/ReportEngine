using MediatR;

using ReportEngine.SharedKernel;
using Templates.Application.Contracts;
using Templates.Application.DTOs;
using Templates.Application.Mapping;
using Templates.Domain.ReportTemplates;

namespace Templates.Application.Features.ReportTemplates.GetAll;

internal sealed class GetAllReportTemplatesQueryHandler
    : IRequestHandler<GetAllReportTemplatesQuery, Result<IReadOnlyList<ReportTemplateDto>>>
{
    private readonly IReportTemplateRepository _repository;

    public GetAllReportTemplatesQueryHandler(IReportTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ReportTemplateDto>>> Handle(
        GetAllReportTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReportTemplate> templates =
            await _repository.GetAllAsync(cancellationToken);

        return Result.Ok<IReadOnlyList<ReportTemplateDto>>(
            templates.Select(t => t.ToDto()).ToList());
    }
}
