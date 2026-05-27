namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for persisting rendered report output files to durable storage.
/// <para>
/// Implementations in <c>Reporting.Infrastructure</c> may target the local file system,
/// Azure Blob Storage, AWS S3, or any other storage provider.
/// </para>
/// </summary>
public interface IReportOutputStorage
{
    /// <summary>
    /// Writes the rendered report content to storage and returns the storage key
    /// (path, blob name, or S3 key) that can later be used to retrieve or serve the file.
    /// </summary>
    /// <param name="executionId">The execution id — used to scope the storage path.</param>
    /// <param name="fileName">Suggested file name including extension.</param>
    /// <param name="content">Raw binary content to persist.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The durable storage path/key for the written file.</returns>
    public Task<string> SaveAsync(
        Guid executionId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default);
}
