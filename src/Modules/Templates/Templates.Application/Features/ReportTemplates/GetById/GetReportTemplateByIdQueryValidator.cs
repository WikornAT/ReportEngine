using FluentValidation;

namespace Templates.Application.Features.ReportTemplates.GetById;

internal sealed class GetReportTemplateByIdQueryValidator : AbstractValidator<GetReportTemplateByIdQuery>
{
    public GetReportTemplateByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Template id is required.");
    }
}
