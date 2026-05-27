using MediatR;

using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.Create;

/// <summary>
/// Creates a new <see cref="Domain.ReportDefinitions.ReportDefinition"/> in Draft status.
/// </summary>
/// <param name="Name">Report display name (max 200 characters).</param>
/// <param name="Category">Logical category grouping.</param>
/// <param name="Description">Optional description.</param>
/// <param name="SubCategory">Optional sub-category.</param>
public sealed record CreateReportDefinitionCommand(
    string Name,
    string Category,
    string? Description,
    string? SubCategory) : IRequest<Result<ReportDefinitionDto>>;
