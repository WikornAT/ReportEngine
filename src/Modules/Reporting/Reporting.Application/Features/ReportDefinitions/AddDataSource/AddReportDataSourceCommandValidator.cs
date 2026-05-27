using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.AddDataSource;

/// <summary>Validates <see cref="AddReportDataSourceCommand"/> inputs.</summary>
public sealed class AddReportDataSourceCommandValidator : AbstractValidator<AddReportDataSourceCommand>
{
    public AddReportDataSourceCommandValidator()
    {
        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Data source name is required.")
            .MaximumLength(100).WithMessage("Data source name must not exceed 100 characters.");

        RuleFor(x => x.DataSourceType)
            .IsInEnum().WithMessage("A valid data source type is required.");

        RuleFor(x => x.ConnectionStringName)
            .NotEmpty().WithMessage("Connection string name is required.")
            .MaximumLength(200).WithMessage("Connection string name must not exceed 200 characters.");

        RuleFor(x => x.QueryText)
            .NotEmpty().WithMessage("Query text is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThan(0).WithMessage("Sort order must be greater than 0.");
    }
}
