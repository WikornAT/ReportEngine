using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.RemoveDataSource;

/// <summary>Validates <see cref="RemoveReportDataSourceCommand"/> inputs.</summary>
public sealed class RemoveReportDataSourceCommandValidator : AbstractValidator<RemoveReportDataSourceCommand>
{
    public RemoveReportDataSourceCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.DataSourceId)
            .NotEmpty().WithMessage("Data source id is required.");
    }
}
