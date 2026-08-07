using Microsoft.Extensions.Options;

using Reporting.Application.Contracts;

namespace Reporting.Infrastructure.Services;

internal sealed class LocalFileReportOutputStorage : IReportOutputStorage
{
    private readonly ReportOutputStorageOptions _options;

    public LocalFileReportOutputStorage(IOptions<ReportOutputStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(
        Guid executionId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        ArgumentNullException.ThrowIfNull(content);

        string rootPath = ResolveRootPath(_options.RootPath);
        string executionFolder = executionId.ToString("N");
        string safeFileName = SanitizeFileName(fileName);

        string targetDirectory = Path.Combine(rootPath, executionFolder);
        Directory.CreateDirectory(targetDirectory);

        string fullPath = Path.Combine(targetDirectory, safeFileName);
        fullPath = EnsureUniquePath(fullPath);

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        string persistedFileName = Path.GetFileName(fullPath);
        return $"{executionFolder}/{persistedFileName}";
    }

    private static string ResolveRootPath(string configuredRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "report-outputs");
        }

        if (Path.IsPathRooted(configuredRootPath))
        {
            return configuredRootPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredRootPath));
    }

    private static string SanitizeFileName(string fileName)
    {
        string onlyName = Path.GetFileName(fileName);

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            onlyName = onlyName.Replace(invalid, '_');
        }

        if (string.IsNullOrWhiteSpace(onlyName))
        {
            return "report.bin";
        }

        return onlyName;
    }

    private static string EnsureUniquePath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return fullPath;
        }

        string directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(fullPath);
        string extension = Path.GetExtension(fullPath);

        string candidate = Path.Combine(directory, $"{name}_{Guid.NewGuid():N}{extension}");
        return candidate;
    }
}
