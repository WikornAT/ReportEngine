using FluentValidation;

namespace Reporting.Application.Features.ReportDefinitions.Update;

/// <summary>Validates <see cref="UpdateReportDefinitionCommand"/> inputs before the handler runs.</summary>
public sealed class UpdateReportDefinitionCommandValidator : AbstractValidator<UpdateReportDefinitionCommand>
{
    public UpdateReportDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Report name is required.")
            .MaximumLength(200).WithMessage("Report name must not exceed 200 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.SubCategory)
            .MaximumLength(100).WithMessage("Sub-category must not exceed 100 characters.")
            .When(x => x.SubCategory is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);
    }
}
