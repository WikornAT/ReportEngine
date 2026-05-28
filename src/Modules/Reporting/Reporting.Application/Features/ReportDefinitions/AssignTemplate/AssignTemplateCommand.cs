using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.AssignTemplate;

/// <summary>
/// Associates a <see cref="Domain.ReportDefinitions.ReportDefinition"/> with a template
/// from the Templates module, enabling HTML-based rendering.
/// </summary>
/// <param name="ReportDefinitionId">The report definition to update.</param>
/// <param name="TemplateId">The id of the <c>ReportTemplate</c> to assign.</param>
public sealed record AssignTemplateCommand(
    Guid ReportDefinitionId,
    Guid TemplateId) : IRequest<Result<ReportDefinitionDto>>;
