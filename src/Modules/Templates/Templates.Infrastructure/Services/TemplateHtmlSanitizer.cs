using System.Text.RegularExpressions;

using Templates.Application.Contracts;

namespace Templates.Infrastructure.Services;

/// <summary>
/// Sanitizes HTML template content by removing dangerous elements and attributes.
/// </summary>
internal sealed class TemplateHtmlSanitizer : ITemplateHtmlSanitizer
{
    // Tags that are completely removed including their content
    private static readonly Regex _scriptTagRegex = new(
        @"<script[\s\S]*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _iframeTagRegex = new(
        @"<iframe[\s\S]*?</iframe>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Inline event handlers: onload="..." onclick="..." etc.
    private static readonly Regex _eventHandlerRegex = new(
        @"\s+on\w+\s*=\s*""[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _eventHandlerSingleQuoteRegex = new(
        @"\s+on\w+\s*=\s*'[^']*'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // javascript: in href/src attributes
    private static readonly Regex _jsProtocolRegex = new(
        @"(href|src|action)\s*=\s*""javascript:[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // file:// URLs
    private static readonly Regex _fileProtocolRegex = new(
        @"(href|src)\s*=\s*""file://[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // External http/https asset references in src/href/url()
    private static readonly Regex _externalSrcRegex = new(
        @"(src|href)\s*=\s*""https?://[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _externalUrlCssRegex = new(
        @"url\(""?https?://[^)""]*""?\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Sanitize(string rawHtml, bool allowExternalAssets = false)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            return rawHtml;
        }

        string html = rawHtml;

        // Remove script blocks
        html = _scriptTagRegex.Replace(html, string.Empty);

        // Remove iframe blocks
        html = _iframeTagRegex.Replace(html, string.Empty);

        // Remove inline event handlers
        html = _eventHandlerRegex.Replace(html, string.Empty);
        html = _eventHandlerSingleQuoteRegex.Replace(html, string.Empty);

        // Remove javascript: protocol
        html = _jsProtocolRegex.Replace(html, "$1=\"#\"");

        // Remove file:// protocol
        html = _fileProtocolRegex.Replace(html, "$1=\"\"");

        if (!allowExternalAssets)
        {
            // Remove external http/https src/href references
            html = _externalSrcRegex.Replace(html, "$1=\"\"");
            html = _externalUrlCssRegex.Replace(html, "url(\"\")");
        }

        return html;
    }
}
