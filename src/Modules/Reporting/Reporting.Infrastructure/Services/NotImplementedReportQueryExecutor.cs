using Reporting.Application.Contracts;

namespace Reporting.Infrastructure.Services;

internal sealed class NotImplementedReportQueryExecutor : IReportQueryExecutor
{
    public Task<string> ExecuteAsync(
        Guid reportDefinitionId,
        string parametersJson,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("IReportQueryExecutor is not yet implemented.");
}
