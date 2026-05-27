using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.Activate;

/// <summary>Validates <see cref="ActivateReportDefinitionCommand"/> inputs.</summary>
public sealed class ActivateReportDefinitionCommandValidator
    : AbstractValidator<ActivateReportDefinitionCommand>
{
    public ActivateReportDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Report definition id is required.");
    }
}
