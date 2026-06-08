using Reporting.Application.Models;

namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for binding a <see cref="TemplateRenderContext"/> into an HTML template string.
/// <para>
/// The implementation in <c>Reporting.Infrastructure</c> uses Scriban and exposes four
/// root namespaces: <c>report</c>, <c>params</c>, <c>data</c>, and <c>system</c>.
/// </para>
/// </summary>
public interface ITemplateBindingEngine
{
    /// <summary>
    /// Binds all values from <paramref name="context"/> into <paramref name="htmlTemplate"/>
    /// and returns the fully resolved HTML string.
    /// </summary>
    /// <param name="htmlTemplate">
    /// Raw HTML template with Scriban placeholders such as
    /// <c>{{ report.name }}</c>, <c>{{ params.invoice_no }}</c>, <c>{{ data.orders }}</c>.
    /// </param>
    /// <param name="context">Structured render context carrying all bindable values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTML string with all placeholders resolved.</returns>
    /// <exception cref="TemplateBindingException">
    /// Thrown on template syntax errors or unresolvable strict bindings.
    /// </exception>
    Task<string> BindAsync(
        string htmlTemplate,
        TemplateRenderContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Thrown when an HTML template has a syntax error or a binding requirement fails.
/// </summary>
public sealed class TemplateBindingException(string message, Exception? inner = null)
    : Exception(message, inner);
