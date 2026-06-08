namespace Templates.Application.Contracts;

/// <summary>
/// Sanitizes raw HTML imported from user-supplied files.
/// <para>
/// Removes or neutralises dangerous constructs including:
/// <list type="bullet">
///   <item><c>&lt;script&gt;</c> tags and inline event handlers</item>
///   <item><c>&lt;iframe&gt;</c> tags</item>
///   <item>External <c>http(s)://</c> asset references (unless options allow them)</item>
///   <item><c>file://</c> URLs</item>
/// </list>
/// </para>
/// </summary>
public interface ITemplateHtmlSanitizer
{
    /// <summary>
    /// Returns a sanitized copy of <paramref name="rawHtml"/>.
    /// </summary>
    /// <param name="rawHtml">Raw HTML string as read from the uploaded file.</param>
    /// <param name="allowExternalAssets">
    /// When <see langword="true"/>, <c>http(s)://</c> references in <c>src</c> and <c>href</c>
    /// attributes are left intact. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>The sanitized HTML string.</returns>
    string Sanitize(string rawHtml, bool allowExternalAssets = false);
}
