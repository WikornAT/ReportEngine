using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportExecutions.Execute;

/// <summary>
/// Triggers the end-to-end execution of a report:
/// validates parameters → queries data → renders output → persists execution record.
/// </summary>
/// <param name="ReportDefinitionId">The report to execute.</param>
/// <param name="ParametersJson">
/// JSON object of parameter values keyed by parameter name (e.g., <c>{"StartDate":"2025-01-01"}</c>).
/// Pass <c>{}</c> when the report declares no parameters.
/// </param>
/// <param name="RequestedFormats">
/// One or more output formats to render. At least one must be provided.
/// </param>
/// <param name="CorrelationId">
/// Optional distributed-trace correlation token supplied by the caller.
/// </param>
public sealed record ExecuteReportCommand(
    Guid ReportDefinitionId,
    string ParametersJson,
    IReadOnlyList<ReportOutputFormat> RequestedFormats,
    string? CorrelationId) : IRequest<Result<ReportExecutionDto>>;
