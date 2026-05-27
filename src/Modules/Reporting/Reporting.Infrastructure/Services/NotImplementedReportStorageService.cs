using Reporting.Application.Contracts;

namespace Reporting.Infrastructure.Services;

internal sealed class NotImplementedReportStorageService : IReportOutputStorage
{
    public Task<string> SaveAsync(
        Guid executionId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("IReportOutputStorage is not yet implemented.");
}
