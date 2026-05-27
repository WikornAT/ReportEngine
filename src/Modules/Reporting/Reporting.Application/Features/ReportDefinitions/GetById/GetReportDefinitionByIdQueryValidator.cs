using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.GetById;

/// <summary>Validates <see cref="GetReportDefinitionByIdQuery"/> inputs.</summary>
public sealed class GetReportDefinitionByIdQueryValidator
    : AbstractValidator<GetReportDefinitionByIdQuery>
{
    public GetReportDefinitionByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Report definition id is required.");
    }
}
