using Reporting.Domain.Common;
using Reporting.Domain.Enums;

namespace Reporting.Domain.ReportExecutions;

/// <summary>
/// Represents a rendered output file produced by a <see cref="ReportExecution"/>.
/// <para>
/// A single execution can produce multiple output files when the caller requests
/// more than one format (e.g., PDF for archival and Excel for download).
/// </para>
/// <para>
/// <see cref="ReportOutputFile"/> is an owned child entity of <see cref="ReportExecution"/>
/// and must only be created through the aggregate root.
/// </para>
/// <para>
/// <b>Extension point:</b> Add <c>ChecksumSha256</c> (string?) for file-integrity verification
/// and <c>RetentionExpiresAt</c> (DateTimeOffset?) for automated purge policies.
/// </para>
/// </summary>
public sealed class ReportOutputFile
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Foreign key to the owning <see cref="ReportExecution"/>.</summary>
    public Guid ReportExecutionId { get; private set; }

    // ── File descriptor ───────────────────────────────────────────────────────

    /// <summary>The render format of this output file.</summary>
    public ReportOutputFormat OutputFormat { get; private set; }

    /// <summary>
    /// Original file name including extension (e.g., <c>MonthlyRevenue_2025-06.pdf</c>).
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Storage key or path used by the infrastructure storage provider to locate the file
    /// (e.g., a blob path, S3 key, or local file system path).
    /// </summary>
    public string StoragePath { get; private set; } = string.Empty;

    /// <summary>MIME type of the file (e.g., <c>application/pdf</c>).</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>File size in bytes.  Set to <c>0</c> when the file has not yet been written.</summary>
    public long FileSizeBytes { get; private set; }

    // ── Timestamps ────────────────────────────────────────────────────────────

    /// <summary>UTC timestamp when the output file was written to storage.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Private parameterless constructor required by EF Core.
    /// Do not use directly; use <see cref="Create"/> instead.
    /// </summary>
    private ReportOutputFile() { }

    // ── Factory ───────────────────────────────────────────────────────────────

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
    /// <returns>A new <see cref="ReportOutputFile"/> instance.</returns>
    internal static ReportOutputFile Create(
        Guid reportExecutionId,
        ReportOutputFormat outputFormat,
        string fileName,
        string storagePath,
        string contentType,
        long fileSizeBytes)
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
            Id = Guid.NewGuid(),
            ReportExecutionId = reportExecutionId,
            OutputFormat = outputFormat,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}
