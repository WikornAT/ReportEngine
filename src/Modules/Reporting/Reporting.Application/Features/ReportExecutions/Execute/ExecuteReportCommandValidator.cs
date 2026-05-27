using FluentValidation;

namespace Reporting.Application.Features.ReportExecutions.Execute;

/// <summary>Validates <see cref="ExecuteReportCommand"/> inputs.</summary>
public sealed class ExecuteReportCommandValidator : AbstractValidator<ExecuteReportCommand>
{
    public ExecuteReportCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.ParametersJson)
            .NotEmpty().WithMessage("Parameters JSON is required (use '{}' for no parameters).");

        RuleFor(x => x.RequestedFormats)
            .NotEmpty().WithMessage("At least one output format must be requested.");

        RuleForEach(x => x.RequestedFormats)
            .IsInEnum().WithMessage("All requested output formats must be valid.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100).WithMessage("Correlation id must not exceed 100 characters.")
            .When(x => x.CorrelationId is not null);
    }
}
