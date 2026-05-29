using Microsoft.EntityFrameworkCore;

using Reporting.Domain.RenderLogs;
using Reporting.Domain.ReportDefinitions;
using Reporting.Domain.ReportExecutions;

namespace Reporting.Application.Contracts;

/// <summary>
/// Abstraction over the Reporting module's EF Core <c>DbContext</c>.
/// Keeps the Application layer free of EF Core infrastructure concerns while still
/// allowing handlers to query and persist aggregates through a single unit-of-work.
/// <para>
/// The infrastructure implementation (<c>ReportingDbContext</c>) must implement this interface.
/// </para>
/// </summary>
public interface IReportingDbContext
{
    /// <summary>The <see cref="ReportDefinition"/> aggregate roots.</summary>
    public DbSet<ReportDefinition> ReportDefinitions { get; }

    /// <summary>The <see cref="ReportExecution"/> aggregate roots.</summary>
    public DbSet<ReportExecution> ReportExecutions { get; }

    /// <summary>Lightweight logs for inline preview and direct PDF renders.</summary>
    public DbSet<RenderLog> RenderLogs { get; }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
