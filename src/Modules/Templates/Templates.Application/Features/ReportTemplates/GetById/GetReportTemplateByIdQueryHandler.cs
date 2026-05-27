using MediatR;

using Exim.ReportEngine.SharedKernel;
using Templates.Application.Contracts;
using Templates.Application.DTOs;
using Templates.Application.Mapping;
using Templates.Domain.ReportTemplates;

namespace Templates.Application.Features.ReportTemplates.GetById;

internal sealed class GetReportTemplateByIdQueryHandler
    : IRequestHandler<GetReportTemplateByIdQuery, Result<ReportTemplateDto>>
{
    private readonly IReportTemplateRepository _repository;

    public GetReportTemplateByIdQueryHandler(IReportTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ReportTemplateDto>> Handle(
        GetReportTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        ReportTemplate? template = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (template is null)
        {
            return AppError.NotFound(nameof(ReportTemplate), request.Id);
        }

        return template.ToDto();
    }
}
