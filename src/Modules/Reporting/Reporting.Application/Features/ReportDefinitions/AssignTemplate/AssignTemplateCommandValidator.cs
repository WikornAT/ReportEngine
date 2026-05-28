using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.AssignTemplate;

internal sealed class AssignTemplateCommandValidator : AbstractValidator<AssignTemplateCommand>
{
    public AssignTemplateCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty();

        RuleFor(x => x.TemplateId)
            .NotEmpty();
    }
}
