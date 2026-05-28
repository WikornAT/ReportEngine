using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.Update;

/// <summary>
/// Updates the metadata of an existing <see cref="Domain.ReportDefinitions.ReportDefinition"/>.
/// </summary>
/// <param name="Id">The id of the report definition to update.</param>
/// <param name="Name">New display name.</param>
/// <param name="Category">New logical category.</param>
/// <param name="Description">New description (pass <see langword="null"/> to clear).</param>
/// <param name="SubCategory">New sub-category (pass <see langword="null"/> to clear).</param>
public sealed record UpdateReportDefinitionCommand(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string? SubCategory) : IRequest<Result<ReportDefinitionDto>>;
