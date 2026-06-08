using System.Text.RegularExpressions;

using Templates.Application.Contracts;
using Templates.Domain.ReportTemplates;

namespace Templates.Infrastructure.Services;

/// <summary>
/// Rewrites relative asset references in HTML and CSS to use the secure
/// <c>/api/v1/templates/{templateId}/assets/{assetId}/content</c> URLs.
/// </summary>
internal sealed class TemplateAssetReferenceRewriter : ITemplateAssetReferenceRewriter
{
    public string RewriteHtml(string html, IReadOnlyList<TemplateAsset> assets)
    {
        if (string.IsNullOrWhiteSpace(html) || assets.Count == 0)
        {
            return html;
        }

        string result = html;

        foreach (TemplateAsset asset in assets)
        {
            result = RewriteAttribute(result, "src", asset.RelativePath, asset.PublicUrl);
            result = RewriteAttribute(result, "href", asset.RelativePath, asset.PublicUrl);
            result = RewriteUrlFunction(result, asset.RelativePath, asset.PublicUrl);
        }

        return result;
    }

    public string RewriteCss(string css, IReadOnlyList<TemplateAsset> assets)
    {
        if (string.IsNullOrWhiteSpace(css) || assets.Count == 0)
        {
            return css;
        }

        string result = css;

        foreach (TemplateAsset asset in assets)
        {
            result = RewriteUrlFunction(result, asset.RelativePath, asset.PublicUrl);
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string RewriteAttribute(
        string content,
        string attribute,
        string relativePath,
        string publicUrl)
    {
        // Match both double and single quoted values, and unquoted paths
        string escapedPath = Regex.Escape(relativePath);

        // Double quotes
        content = Regex.Replace(
            content,
            $@"({attribute}\s*=\s*""){escapedPath}("")",
            $"$1{publicUrl}$2",
            RegexOptions.IgnoreCase);

        // Single quotes
        content = Regex.Replace(
            content,
            $@"({attribute}\s*=\s*'){escapedPath}(')",
            $"$1{publicUrl}$2",
            RegexOptions.IgnoreCase);

        return content;
    }

    private static string RewriteUrlFunction(
        string content,
        string relativePath,
        string publicUrl)
    {
        string escapedPath = Regex.Escape(relativePath);

        // url("path"), url('path'), url(path)
        content = Regex.Replace(
            content,
            $@"url\(""{escapedPath}""\)",
            $"url(\"{publicUrl}\")",
            RegexOptions.IgnoreCase);

        content = Regex.Replace(
            content,
            $@"url\('{escapedPath}'\)",
            $"url('{publicUrl}')",
            RegexOptions.IgnoreCase);

        content = Regex.Replace(
            content,
            $@"url\({escapedPath}\)",
            $"url({publicUrl})",
            RegexOptions.IgnoreCase);

        return content;
    }
}
