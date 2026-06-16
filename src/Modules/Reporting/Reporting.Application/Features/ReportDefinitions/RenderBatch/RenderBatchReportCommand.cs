using MediatR;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RenderBatch;

/// <summary>
/// Renders a report for one or more parameter sets according to the specified
/// <see cref="RenderMode"/> and returns the combined output as a
/// <see cref="BatchRenderResult"/>.
/// </summary>
/// <param name="ReportDefinitionId">The report to render.</param>
/// <param name="RenderMode">How to combine the per-item outputs.</param>
/// <param name="ParametersJsonItems">
/// Ordered list of per-item JSON parameter objects.
/// Single mode requires exactly one element; other modes accept multiple.
/// </param>
/// <param name="OutputFileNamePattern">
/// Optional pattern for individual output file names.
/// Supports <c>{reportName}</c>, <c>{index}</c>, <c>{batchExecutionId}</c>.
/// </param>
/// <param name="ContinueOnError">
/// When <see langword="true"/>, failed items are skipped for
/// <see cref="RenderMode.ZipPdf"/> and <see cref="RenderMode.PreviewHtml"/>.
/// Has no effect for <see cref="RenderMode.Single"/> and <see cref="RenderMode.MergePdf"/>.
/// </param>
/// <param name="TriggeredBy">Identity of the caller for audit logs.</param>
public sealed record RenderBatchReportCommand(
    Guid ReportDefinitionId,
    RenderMode RenderMode,
    IReadOnlyList<string> ParametersJsonItems,
    string? OutputFileNamePattern = null,
    bool ContinueOnError = false,
    string TriggeredBy = "system") : IRequest<Result<BatchRenderResult>>;
