using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportDefinitions.GetList;

/// <summary>
/// Returns a paged, filtered list of <see cref="Domain.ReportDefinitions.ReportDefinition"/> summaries.
/// All filters are optional and combined with AND logic.
/// </summary>
/// <param name="Category">Filter by category (case-insensitive, partial match).</param>
/// <param name="SearchTerm">Filter by name substring (case-insensitive).</param>
/// <param name="Status">Filter by lifecycle status.</param>
/// <param name="IncludeHidden">When <see langword="true"/>, includes hidden reports in results.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Number of results per page (max 100).</param>
public sealed record GetReportDefinitionsQuery(
    string? Category,
    string? SearchTerm,
    ReportStatus? Status,
    bool IncludeHidden,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<ReportDefinitionDto>>>;
