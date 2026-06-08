namespace Templates.Infrastructure.Options;

/// <summary>Configuration options for template asset file storage.</summary>
public sealed class TemplateStorageOptions
{
    public const string SectionName = "TemplateStorage";

    /// <summary>
    /// Absolute or web-root-relative path under which template asset files are stored.
    /// Default: <c>wwwroot/template-assets</c> relative to the application base path.
    /// </summary>
    public string AssetRootPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "wwwroot", "template-assets");
}
