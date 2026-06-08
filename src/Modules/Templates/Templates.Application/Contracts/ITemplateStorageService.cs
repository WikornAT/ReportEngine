namespace Templates.Application.Contracts;

/// <summary>
/// Persists and retrieves raw asset bytes for a template.
/// <para>
/// The implementation in <c>Templates.Infrastructure</c> stores files under a controlled
/// directory root. Physical paths are never surfaced to callers.
/// </para>
/// </summary>
public interface ITemplateStorageService
{
    /// <summary>
    /// Saves <paramref name="content"/> and returns the absolute storage path.
    /// </summary>
    /// <param name="templateId">Owning template id — used to scope the storage directory.</param>
    /// <param name="safeFileName">Sanitized file name (no path separators, no traversal).</param>
    /// <param name="content">Raw file bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absolute file-system path where the bytes were written.</returns>
    Task<string> SaveAssetAsync(
        Guid templateId,
        string safeFileName,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the bytes of a previously saved asset.</summary>
    /// <param name="storagePath">Absolute storage path returned by <see cref="SaveAssetAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<byte[]> ReadAssetAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes all assets stored for the given template.</summary>
    Task DeleteTemplateAssetsAsync(Guid templateId, CancellationToken cancellationToken = default);
}
