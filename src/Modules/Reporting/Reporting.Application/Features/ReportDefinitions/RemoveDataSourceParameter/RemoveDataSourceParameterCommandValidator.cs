using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.RemoveDataSourceParameter;

/// <summary>Validates <see cref="RemoveDataSourceParameterCommand"/> inputs.</summary>
public sealed class RemoveDataSourceParameterCommandValidator : AbstractValidator<RemoveDataSourceParameterCommand>
{
    public RemoveDataSourceParameterCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.DataSourceId)
            .NotEmpty().WithMessage("Data source id is required.");

        RuleFor(x => x.ParameterId)
            .NotEmpty().WithMessage("Parameter id is required.");
    }
}
