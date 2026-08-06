using FluentValidation;

using Microsoft.Extensions.Options;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportDefinitions.RenderBatch;

/// <summary>Validates <see cref="RenderBatchReportCommand"/> inputs.</summary>
internal sealed class RenderBatchReportCommandValidator : AbstractValidator<RenderBatchReportCommand>
{
    public RenderBatchReportCommandValidator(IOptions<BatchRenderOptions> options)
    {
        int maxBatchSize = options.Value.MaxBatchSize;

        RuleFor(x => x.ReportDefinitionId)
            .NotEmpty().WithMessage("Report definition id is required.");

        RuleFor(x => x.RenderMode)
            .IsInEnum().WithMessage("RenderMode must be a valid value (SingleFile, MergePdf, ZipPdf, PreviewHtml).");

        RuleFor(x => x.ParametersJsonItems)
            .NotEmpty().WithMessage("At least one parameter object must be supplied.");

        RuleFor(x => x.ParametersJsonItems)
            .Must(items => items.Count <= maxBatchSize)
            .WithMessage($"Batch size must not exceed {maxBatchSize} items.")
            .When(x => x.ParametersJsonItems.Count > 0);

        RuleFor(x => x.ParametersJsonItems)
            .Must(items => items.Count == 1)
            .WithMessage("Single file mode requires exactly one parameter object.")
            .When(x => x.RenderMode == RenderMode.SingleFile && x.ParametersJsonItems.Count > 0);

        RuleFor(x => x.ContinueOnError)
            .Must(v => !v)
            .WithMessage("ContinueOnError is only supported for ZipPdf and PreviewHtml modes.")
            .When(x => x.ContinueOnError
                && x.RenderMode is not (RenderMode.ZipPdf or RenderMode.PreviewHtml));

        RuleForEach(x => x.ParametersJsonItems)
            .Must(BeValidJsonObject)
            .WithMessage("Each parameters item must be a valid JSON object.");
    }

    private static bool BeValidJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }
}
