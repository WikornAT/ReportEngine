using Microsoft.EntityFrameworkCore;

using Templates.Application.Contracts;
using Templates.Domain.ReportTemplates;
using Templates.Infrastructure.Persistence;

namespace Templates.Infrastructure.Repositories;

internal sealed class ReportTemplateRepository : IReportTemplateRepository
{
    private readonly TemplatesDbContext _dbContext;

    public ReportTemplateRepository(TemplatesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.ReportTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReportTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.ReportTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public void Add(ReportTemplate reportTemplate) =>
        _dbContext.ReportTemplates.Add(reportTemplate);
}
