using Templates.Domain.ReportTemplates;

namespace Templates.Application.Contracts;

/// <summary>
/// Read/write repository for <see cref="ReportTemplate"/> aggregates.
/// Implemented in <c>Templates.Infrastructure</c>.
/// </summary>
public interface IReportTemplateRepository
{
    /// <summary>Returns the template with the given id, or <see langword="null"/> if not found.</summary>
    Task<ReportTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all templates (untracked).</summary>
    Task<IReadOnlyList<ReportTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new template to the context (not yet persisted).</summary>
    void Add(ReportTemplate reportTemplate);
}
