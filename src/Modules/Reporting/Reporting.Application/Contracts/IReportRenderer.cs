using Reporting.Domain.Enums;

namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for rendering a report data payload into one or more output files.
/// <para>
/// Implementations live in <c>Reporting.Infrastructure</c> and delegate to the
/// underlying report engine (e.g., RDLC, Crystal Reports, Telerik Reporting, custom HTML).
/// </para>
/// </summary>
public interface IReportRenderer
{
    /// <summary>
    /// Renders the given data payload using the specified report template and returns
    /// the resulting binary output.
    /// </summary>
    /// <param name="reportDefinitionId">
    /// The id of the report definition whose <c>TemplatePath</c> will be used for rendering.
    /// </param>
    /// <param name="dataJson">
    /// JSON payload produced by <see cref="IReportQueryExecutor.ExecuteAsync"/>.
    /// </param>
    /// <param name="outputFormat">The desired output format.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>A <see cref="RenderedReport"/> describing the rendered output.</returns>
    public Task<RenderedReport> RenderAsync(
        Guid reportDefinitionId,
        string dataJson,
        ReportOutputFormat outputFormat,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Value object returned by <see cref="IReportRenderer.RenderAsync"/>, carrying the
/// rendered binary content and storage metadata.
/// </summary>
/// <param name="FileName">Suggested file name including extension.</param>
/// <param name="ContentType">MIME type of the rendered output.</param>
/// <param name="Content">Raw binary content of the rendered file.</param>
/// <param name="PhaseTimings">Per-phase timing breakdown; may be null when not instrumented.</param>
public sealed record RenderedReport(
    string FileName,
    string ContentType,
    byte[] Content,
    RenderPhaseTimings? PhaseTimings = null);

/// <summary>Per-phase timing breakdown for a single render operation.</summary>
/// <param name="DataSourceExecutionMs">Time spent executing data source queries.</param>
/// <param name="TemplateBindingMs">Time spent in the Scriban binding engine.</param>
/// <param name="RenderMs">Time spent in the PDF renderer (Playwright). Zero for HTML previews.</param>
public sealed record RenderPhaseTimings(
    long DataSourceExecutionMs,
    long TemplateBindingMs,
    long RenderMs);
