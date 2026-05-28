using Templates.Domain.Enums;

namespace Templates.Domain.ReportTemplates;

/// <summary>
/// Aggregate root representing an HTML-based report template.
/// <para>
/// Stores the full HTML and optional supplementary CSS that make up the visual layout of a report.
/// At render time the HTML is processed by a headless browser (Playwright/Chromium) to produce
/// the final PDF or HTML output.
/// </para>
/// <para><b>Invariants:</b>
/// <list type="bullet">
///   <item><see cref="Name"/> is non-empty and unique across all templates.</item>
///   <item><see cref="HtmlContent"/> must be non-empty.</item>
///   <item>Only <see cref="TemplateStatus.Active"/> templates may be used for rendering.</item>
///   <item>Archived templates are immutable.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ReportTemplate
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    // ── Descriptor ───────────────────────────────────────────────────────────

    /// <summary>Human-readable name used to locate this template (unique).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional description of the template's purpose.</summary>
    public string? Description { get; private set; }

    // ── Content ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Full HTML document (including &lt;!DOCTYPE html&gt;, @font-face declarations,
    /// absolute-positioned fields, background images, and Thai font references).
    /// Handlebars-style <c>{{field}}</c> tokens are replaced with report data at render time.
    /// </summary>
    public string HtmlContent { get; private set; } = string.Empty;

    /// <summary>
    /// Optional supplementary CSS injected into the &lt;head&gt; at render time,
    /// after the CSS embedded inside <see cref="HtmlContent"/>.
    /// </summary>
    public string? CssContent { get; private set; }

    // ── Page layout ───────────────────────────────────────────────────────────

    /// <summary>Target paper size for PDF rendering.</summary>
    public PaperSize PaperSize { get; private set; }

    /// <summary>Page orientation for PDF rendering.</summary>
    public PageOrientation Orientation { get; private set; }

    /// <summary>
    /// Design-time canvas width in pixels (e.g., 794 for A4 portrait at 96 dpi).
    /// Used to size the Playwright viewport so the layout renders exactly as designed.
    /// </summary>
    public int WidthPx { get; private set; }

    /// <summary>
    /// Design-time canvas height in pixels (e.g., 1123 for A4 portrait at 96 dpi).
    /// </summary>
    public int HeightPx { get; private set; }

    // ── Versioning ────────────────────────────────────────────────────────────

    /// <summary>
    /// Monotonically increasing version number.
    /// Incremented on every <see cref="UpdateContent"/> call to support audit and rollback.
    /// </summary>
    public int Version { get; private set; }

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Current lifecycle status of this template.</summary>
    public TemplateStatus Status { get; private set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    // ── ORM constructor ───────────────────────────────────────────────────────

    private ReportTemplate() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="ReportTemplate"/> in <see cref="TemplateStatus.Draft"/> status.
    /// </summary>
    public static ReportTemplate Create(
        string name,
        string htmlContent,
        string createdBy,
        string? description = null,
        string? cssContent = null,
        PaperSize paperSize = PaperSize.A4,
        PageOrientation orientation = PageOrientation.Portrait,
        int widthPx = 794,
        int heightPx = 1123)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new ReportTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            HtmlContent = htmlContent,
            CssContent = cssContent,
            PaperSize = paperSize,
            Orientation = orientation,
            WidthPx = widthPx,
            HeightPx = heightPx,
            Version = 1,
            Status = TemplateStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Publishes the template, making it available for rendering.</summary>
    public void Publish(string modifiedBy)
    {
        EnsureNotArchived();
        Status = TemplateStatus.Active;
        Touch(modifiedBy);
    }

    /// <summary>Archives the template. Archived templates are immutable.</summary>
    public void Archive(string modifiedBy)
    {
        EnsureNotArchived();
        Status = TemplateStatus.Archived;
        Touch(modifiedBy);
    }

    // ── Content mutation ──────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the HTML/CSS content and updates layout dimensions.
    /// Each call increments <see cref="Version"/>.
    /// </summary>
    public void UpdateContent(
        string htmlContent,
        string? cssContent,
        string? description,
        PaperSize paperSize,
        PageOrientation orientation,
        int widthPx,
        int heightPx,
        string modifiedBy)
    {
        EnsureNotArchived();
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);

        HtmlContent = htmlContent;
        CssContent = cssContent;
        Description = description;
        PaperSize = paperSize;
        Orientation = orientation;
        WidthPx = widthPx;
        HeightPx = heightPx;
        Version++;
        Touch(modifiedBy);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnsureNotArchived()
    {
        if (Status == TemplateStatus.Archived)
        {
            throw new InvalidOperationException(
                $"Template '{Name}' is Archived and cannot be modified.");
        }
    }

    private void Touch(string modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modifiedBy);
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = modifiedBy;
    }
}
