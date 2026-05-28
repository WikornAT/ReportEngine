using MediatR;

using ReportEngine.SharedKernel;
using Templates.Application.DTOs;

namespace Templates.Application.Features.ReportTemplates.GetById;

/// <summary>Returns a single <see cref="Domain.ReportTemplates.ReportTemplate"/> by id.</summary>
/// <param name="Id">The template id to look up.</param>
public sealed record GetReportTemplateByIdQuery(Guid Id) : IRequest<Result<ReportTemplateDto>>;
