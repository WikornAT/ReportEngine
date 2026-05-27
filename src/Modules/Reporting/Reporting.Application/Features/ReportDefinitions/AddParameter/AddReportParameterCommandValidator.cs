using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.AddParameter;

/// <summary>Validates <see cref="AddReportParameterCommand"/> inputs.</summary>
public sealed class AddReportParameterCommandValidator : AbstractValidator<AddReportParameterCommand>
{
    public AddReportParameterCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Parameter name is required.")
            .MaximumLength(100).WithMessage("Parameter name must not exceed 100 characters.")
            .Matches(@"^[A-Za-z_][A-Za-z0-9_]*$")
            .WithMessage("Parameter name must start with a letter or underscore and contain only alphanumeric characters and underscores.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters.");

        RuleFor(x => x.ParameterType)
            .IsInEnum().WithMessage("A valid parameter type is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThan(0).WithMessage("Sort order must be greater than 0.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.DefaultValue)
            .MaximumLength(1000).WithMessage("Default value must not exceed 1000 characters.")
            .When(x => x.DefaultValue is not null);
    }
}
