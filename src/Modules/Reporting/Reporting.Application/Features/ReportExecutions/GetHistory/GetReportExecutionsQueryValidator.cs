using FluentValidation;

namespace Reporting.Application.Features.ReportExecutions.GetHistory;

/// <summary>Validates <see cref="GetReportExecutionsQuery"/> inputs.</summary>
public sealed class GetReportExecutionsQueryValidator : AbstractValidator<GetReportExecutionsQuery>
{
    public GetReportExecutionsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.TriggeredBy)
            .MaximumLength(200).WithMessage("TriggeredBy filter must not exceed 200 characters.")
            .When(x => x.TriggeredBy is not null);
    }
}
