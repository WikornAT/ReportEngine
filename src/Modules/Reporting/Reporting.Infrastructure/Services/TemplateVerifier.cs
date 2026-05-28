using Reporting.Application.Contracts;

using Templates.Application.Contracts;
using Templates.Domain.Enums;
using Templates.Domain.ReportTemplates;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Implements <see cref="ITemplateVerifier"/> by delegating to
/// <see cref="IReportTemplateRepository"/> from the Templates module.
/// Lives in Reporting.Infrastructure — the only layer permitted to
/// reference Templates.Application directly.
/// </summary>
internal sealed class TemplateVerifier : ITemplateVerifier
{
    private readonly IReportTemplateRepository _templateRepository;

    public TemplateVerifier(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        ReportTemplate? template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        return template is not null;
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        ReportTemplate? template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        return template?.Status == TemplateStatus.Active;
    }
}
