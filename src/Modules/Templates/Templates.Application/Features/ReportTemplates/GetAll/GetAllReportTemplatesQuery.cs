using MediatR;

using ReportEngine.SharedKernel;
using Templates.Application.DTOs;

namespace Templates.Application.Features.ReportTemplates.GetAll;

/// <summary>Returns all report templates.</summary>
public sealed record GetAllReportTemplatesQuery : IRequest<Result<IReadOnlyList<ReportTemplateDto>>>;
