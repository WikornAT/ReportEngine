using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.GetList;

/// <summary>Validates <see cref="GetReportDefinitionsQuery"/> inputs.</summary>
public sealed class GetReportDefinitionsQueryValidator : AbstractValidator<GetReportDefinitionsQuery>
{
    public GetReportDefinitionsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200).WithMessage("Search term must not exceed 200 characters.")
            .When(x => x.SearchTerm is not null);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category filter must not exceed 100 characters.")
            .When(x => x.Category is not null);
    }
}
