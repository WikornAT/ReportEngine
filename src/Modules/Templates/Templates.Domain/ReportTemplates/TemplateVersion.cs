namespace Templates.Domain.ReportTemplates;

/// <summary>
/// An immutable snapshot of a <see cref="ReportTemplate"/>'s HTML and CSS content
/// at the point in time it was imported or updated.
/// <para>
/// Provides a full audit trail and enables rollback to any prior version.
/// </para>
/// </summary>
public sealed class TemplateVersion
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>FK to the owning <see cref="ReportTemplate"/>.</summary>
    public Guid TemplateId { get; private set; }

    // ── Version info ──────────────────────────────────────────────────────────

    /// <summary>Monotonically increasing version number copied from the template at snapshot time.</summary>
    public int Version { get; private set; }

    // ── Snapshot ──────────────────────────────────────────────────────────────

    /// <summary>Full HTML content at this version.</summary>
    public string HtmlContent { get; private set; } = string.Empty;

    /// <summary>CSS content at this version (may be <see langword="null"/>).</summary>
    public string? CssContent { get; private set; }

    /// <summary>
    /// Optional JSON blob capturing supplementary metadata at import time
    /// (e.g., paper size, orientation, source file name, importer options).
    /// </summary>
    public string? SnapshotJson { get; private set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    // ── ORM constructor ───────────────────────────────────────────────────────

    private TemplateVersion() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates a new version snapshot of the given <paramref name="template"/>.</summary>
    internal static TemplateVersion Snapshot(
        ReportTemplate template,
        string createdBy,
        string? snapshotJson = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new TemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            Version = template.Version,
            HtmlContent = template.HtmlContent,
            CssContent = template.CssContent,
            SnapshotJson = snapshotJson,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }
}
