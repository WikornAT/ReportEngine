using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportExecutions;

/// <summary>
/// Represents a rendered output file produced by a <see cref="ReportExecution"/>.
/// Owned child entity — must only be created through the aggregate root.
/// </summary>
public sealed class ReportOutputFile
{
    public Guid Id { get; private set; }
    public Guid ReportExecutionId { get; private set; }
    public ReportOutputFormat OutputFormat { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }

    /// <summary>UTC timestamp when the output file was written to storage.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    private ReportOutputFile() { }

    /// <summary>
    /// Creates a new <see cref="ReportOutputFile"/> child entity.
    /// Called exclusively by <see cref="ReportExecution.AddOutputFile"/>.
    /// </summary>
    /// <param name="reportExecutionId">Id of the owning aggregate root.</param>
    /// <param name="outputFormat">Render format of the file.</param>
    /// <param name="fileName">Original file name with extension (non-empty, max 260 characters).</param>
    /// <param name="storagePath">Storage key or path (non-empty).</param>
    /// <param name="contentType">MIME type (non-empty).</param>
    /// <param name="fileSizeBytes">File size in bytes (must be &gt;= 0).</param>
    /// <param name="now">Current UTC timestamp supplied by the caller — not read from the system clock.</param>
    /// <returns>A new <see cref="ReportOutputFile"/> instance.</returns>
    internal static ReportOutputFile Create(
        Guid reportExecutionId,
        ReportOutputFormat outputFormat,
        string fileName,
        string storagePath,
        string contentType,
        long fileSizeBytes,
        DateTimeOffset now)
    {
        Guard.NotNullOrWhiteSpace(fileName, nameof(fileName));
        Guard.NotNullOrWhiteSpace(storagePath, nameof(storagePath));
        Guard.NotNullOrWhiteSpace(contentType, nameof(contentType));
        Guard.DefinedEnum(outputFormat, nameof(outputFormat));

        if (fileName.Length > 260)
        {
            throw new ReportingDomainException($"'{nameof(fileName)}' must not exceed 260 characters.");
        }

        if (fileSizeBytes < 0)
        {
            throw new ReportingDomainException($"'{nameof(fileSizeBytes)}' must be non-negative.");
        }

        return new ReportOutputFile
        {
            Id                = Guid.NewGuid(),
            ReportExecutionId = reportExecutionId,
            OutputFormat      = outputFormat,
            FileName          = fileName,
            StoragePath       = storagePath,
            ContentType       = contentType,
            FileSizeBytes     = fileSizeBytes,
            GeneratedAt       = now,             // ← caller-supplied, no UtcNow here
        };
    }
}
