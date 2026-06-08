using Templates.Domain.ReportTemplates;

namespace Templates.Application.Contracts;

/// <summary>
/// Rewrites local asset references inside HTML and CSS content so they point to
/// the secure API endpoint after a template has been imported.
/// <para>
/// Example: <c>src="assets/logo.png"</c> → <c>src="/api/templates/report-templates/{id}/assets/{assetId}/content"</c>
/// </para>
/// </summary>
public interface ITemplateAssetReferenceRewriter
{
    /// <summary>
    /// Replaces relative asset references in <paramref name="html"/> with their corresponding
    /// <see cref="TemplateAsset.PublicUrl"/> values, based on the <paramref name="assets"/> lookup.
    /// </summary>
    /// <param name="html">The raw or sanitized HTML string to rewrite.</param>
    /// <param name="assets">
    /// All assets registered for the template, keyed by their original
    /// <see cref="TemplateAsset.RelativePath"/>.
    /// </param>
    /// <returns>The HTML string with all matched references rewritten.</returns>
    string RewriteHtml(string html, IReadOnlyList<TemplateAsset> assets);

    /// <summary>
    /// Replaces relative asset references in <paramref name="css"/> with their corresponding
    /// <see cref="TemplateAsset.PublicUrl"/> values.
    /// </summary>
    /// <param name="css">The raw CSS string to rewrite.</param>
    /// <param name="assets">All assets for the template, keyed by relative path.</param>
    /// <returns>The CSS string with all matched references rewritten.</returns>
    string RewriteCss(string css, IReadOnlyList<TemplateAsset> assets);
}
