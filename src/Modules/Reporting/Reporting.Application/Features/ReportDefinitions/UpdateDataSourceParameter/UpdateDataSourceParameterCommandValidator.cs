using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.UpdateDataSourceParameter;

/// <summary>Validates <see cref="UpdateDataSourceParameterCommand"/> inputs.</summary>
public sealed class UpdateDataSourceParameterCommandValidator : AbstractValidator<UpdateDataSourceParameterCommand>
{
    public UpdateDataSourceParameterCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.DataSourceId)
            .NotEmpty().WithMessage("Data source id is required.");

        RuleFor(x => x.ParameterId)
            .NotEmpty().WithMessage("Parameter id is required.");

        RuleFor(x => x.SourceParameterName)
            .NotEmpty().WithMessage("Source parameter name is required.")
            .MaximumLength(100).WithMessage("Source parameter name must not exceed 100 characters.");

        RuleFor(x => x.ReportParameterName)
            .NotEmpty().WithMessage("Report parameter name is required.")
            .MaximumLength(100).WithMessage("Report parameter name must not exceed 100 characters.");

        RuleFor(x => x.DbType)
            .MaximumLength(50).WithMessage("DbType must not exceed 50 characters.")
            .When(x => x.DbType is not null);
    }
}
