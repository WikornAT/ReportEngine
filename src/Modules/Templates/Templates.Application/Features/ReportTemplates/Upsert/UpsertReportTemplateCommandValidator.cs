using FluentValidation;

namespace Templates.Application.Features.ReportTemplates.Upsert;

internal sealed class UpsertReportTemplateCommandValidator : AbstractValidator<UpsertReportTemplateCommand>
{
    public UpsertReportTemplateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required.")
            .MaximumLength(200).WithMessage("Template name must not exceed 200 characters.");

        RuleFor(x => x.TemplateCode)
            .MaximumLength(100).WithMessage("TemplateCode must not exceed 100 characters.")
            .Matches(@"^[A-Za-z0-9_\-\.]*$").When(x => x.TemplateCode is not null)
            .WithMessage("TemplateCode may only contain letters, digits, hyphens, underscores, and dots.");

        RuleFor(x => x.HtmlContent)
            .NotEmpty().WithMessage("HTML content is required.");

        RuleFor(x => x.WidthPx)
            .GreaterThan(0).WithMessage("WidthPx must be a positive integer.");

        RuleFor(x => x.HeightPx)
            .GreaterThan(0).WithMessage("HeightPx must be a positive integer.");
    }
}
