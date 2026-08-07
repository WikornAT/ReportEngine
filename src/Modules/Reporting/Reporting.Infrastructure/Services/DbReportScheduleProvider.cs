using Microsoft.EntityFrameworkCore;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Services;

internal sealed class DbReportScheduleProvider : IReportScheduleProvider
{
    private readonly ReportingDbContext _dbContext;

    public DbReportScheduleProvider(ReportingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ScheduledReportDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ScheduledReports
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new ScheduledReportDefinition(
                Id: x.Id,
                ReportDefinitionId: x.ReportDefinitionId,
                ScheduleType: x.ScheduleType,
                ParametersJson: string.IsNullOrWhiteSpace(x.ParametersJson) ? "{}" : x.ParametersJson,
                RequestedFormats: ParseFormats(x.RequestedFormatsCsv),
                TriggeredBy: string.IsNullOrWhiteSpace(x.TriggeredBy) ? "system" : x.TriggeredBy))
            .ToList();
    }

    private static List<ReportOutputFormat> ParseFormats(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new List<ReportOutputFormat> { ReportOutputFormat.Pdf };
        }

        var parsed = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Enum.TryParse<ReportOutputFormat>(value, ignoreCase: true, out var format)
                ? format
                : (ReportOutputFormat?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        return parsed.Count == 0 ? [ReportOutputFormat.Pdf] : parsed;
    }
}
