using MediatR;

using Exim.ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.AssignTemplate;

/// <summary>
/// Associates a <see cref="Domain.ReportDefinitions.ReportDefinition"/> with a template
/// from the Templates module, enabling HTML-based rendering.
/// </summary>
/// <param name="ReportDefinitionId">The report definition to update.</param>
/// <param name="TemplateId">The id of the <c>ReportTemplate</c> to assign.</param>
/// <param name="TemplatePath">
/// Optional storage path or descriptive key for the template (e.g., a relative URL or
/// a display name). Persisted on the definition for reference; not used by the HTML renderer.
/// </param>
public sealed record AssignTemplateCommand(
    Guid ReportDefinitionId,
    Guid TemplateId,
    string TemplatePath) : IRequest<Result<ReportDefinitionDto>>;
