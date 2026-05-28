using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.GetDataSources;

/// <summary>Validates <see cref="GetReportDataSourcesQuery"/> inputs.</summary>
public sealed class GetReportDataSourcesQueryValidator : AbstractValidator<GetReportDataSourcesQuery>
{
    public GetReportDataSourcesQueryValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");
    }
}
