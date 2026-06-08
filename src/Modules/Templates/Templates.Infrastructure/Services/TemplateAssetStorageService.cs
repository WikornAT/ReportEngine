using Microsoft.Extensions.Options;

using Templates.Application.Contracts;
using Templates.Infrastructure.Options;

namespace Templates.Infrastructure.Services;

/// <summary>
/// Stores template asset files on the local file system under a controlled root directory.
/// Physical paths are never surfaced outside this class.
/// </summary>
internal sealed class TemplateAssetStorageService : ITemplateStorageService
{
    private readonly string _rootPath;

    public TemplateAssetStorageService(IOptions<TemplateStorageOptions> options)
    {
        _rootPath = options.Value.AssetRootPath;
    }

    public async Task<string> SaveAssetAsync(
        Guid templateId,
        string safeFileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        string dir = GetTemplateDir(templateId);
        Directory.CreateDirectory(dir);

        // Prevent overwriting by appending a short hash if a file with the same name exists
        string fullPath = Path.Combine(dir, safeFileName);
        if (File.Exists(fullPath))
        {
            string stem = Path.GetFileNameWithoutExtension(safeFileName);
            string ext = Path.GetExtension(safeFileName);
            fullPath = Path.Combine(dir, $"{stem}_{Guid.NewGuid():N}{ext}");
        }

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return fullPath;
    }

    public async Task<byte[]> ReadAssetAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException(
                $"Asset file not found at storage path '{storagePath}'.", storagePath);
        }

        return await File.ReadAllBytesAsync(storagePath, cancellationToken);
    }

    public Task DeleteTemplateAssetsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        string dir = GetTemplateDir(templateId);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string GetTemplateDir(Guid templateId) =>
        Path.Combine(_rootPath, templateId.ToString("N"));
}
