using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.Deactivate;

/// <summary>Validates <see cref="DeactivateReportDefinitionCommand"/> inputs.</summary>
public sealed class DeactivateReportDefinitionCommandValidator
    : AbstractValidator<DeactivateReportDefinitionCommand>
{
    public DeactivateReportDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Report definition id is required.");
    }
}
