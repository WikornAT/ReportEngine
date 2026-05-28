using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Application.Features.ReportDefinitions.GetList;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportExecutions.GetHistory;

/// <summary>
/// Returns a paged, filtered list of <see cref="Domain.ReportExecutions.ReportExecution"/> records.
/// All filters are optional and combined with AND logic.
/// </summary>
/// <param name="ReportDefinitionId">Filter executions for a specific report definition.</param>
/// <param name="TriggeredBy">Filter executions by the identity that triggered them.</param>
/// <param name="Status">Filter by execution lifecycle status.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Number of results per page (max 100).</param>
public sealed record GetReportExecutionsQuery(
    Guid? ReportDefinitionId,
    string? TriggeredBy,
    ReportExecutionStatus? Status,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<ReportExecutionDto>>>;
