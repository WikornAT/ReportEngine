using Reporting.Application.Contracts;
using Reporting.Domain.Enums;

namespace Reporting.Infrastructure.Services;

internal sealed class NotImplementedReportRenderer : IReportRenderer
{
    public Task<RenderedReport> RenderAsync(
        Guid reportDefinitionId,
        string dataJson,
        ReportOutputFormat outputFormat,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("IReportRenderer is not yet implemented.");
}
