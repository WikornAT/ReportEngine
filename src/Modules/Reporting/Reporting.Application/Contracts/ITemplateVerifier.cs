namespace Reporting.Application.Contracts;

/// <summary>
/// Cross-module abstraction that lets the Reporting Application layer verify
/// template state without taking a direct dependency on the Templates module.
/// Implemented in <c>Reporting.Infrastructure</c>, which has access to
/// <c>Templates.Application.Contracts.IReportTemplateRepository</c>.
/// </summary>
public interface ITemplateVerifier
{
    /// <summary>
    /// Returns <see langword="true"/> when a template with <paramref name="templateId"/>
    /// exists in the Templates module store.
    /// </summary>
    Task<bool> ExistsAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when the template is in <c>Active</c> status
    /// and therefore eligible to be assigned for rendering.
    /// Returns <see langword="false"/> when the template does not exist or is Draft/Archived.
    /// </summary>
    Task<bool> IsActiveAsync(Guid templateId, CancellationToken cancellationToken = default);
}
