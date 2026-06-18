namespace Reporting.Application.Contracts;

/// <summary>
/// Configuration options for batch report rendering.
/// Bind from <c>appsettings.json</c> under <c>"Reporting:BatchRender"</c>.
/// </summary>
public sealed class BatchRenderOptions
{
    public const string SectionName = "Reporting:BatchRender";

    /// <summary>
    /// Maximum number of parameter objects allowed in a single batch render request.
    /// Defaults to <c>100</c>.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;
}
