using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.GetDataSourceById;

/// <summary>Validates <see cref="GetReportDataSourceByIdQuery"/> inputs.</summary>
public sealed class GetReportDataSourceByIdQueryValidator : AbstractValidator<GetReportDataSourceByIdQuery>
{
    public GetReportDataSourceByIdQueryValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.DataSourceId)
            .NotEmpty().WithMessage("Data source id is required.");
    }
}
