namespace Reporting.Infrastructure.Services;

/// <summary>
/// Configuration options for local report output storage.
/// Bind from <c>appsettings.json</c> under <c>"Reporting:OutputStorage"</c>.
/// </summary>
public sealed class ReportOutputStorageOptions
{
    public const string SectionName = "Reporting:OutputStorage";

    /// <summary>
    /// Absolute or relative root path where rendered report files are persisted.
    /// Default is a <c>report-outputs</c> folder under the app base directory.
    /// </summary>
    public string RootPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "report-outputs");
}
