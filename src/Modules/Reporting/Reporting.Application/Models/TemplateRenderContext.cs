using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Models;

/// <summary>
/// The structured context object passed to <see cref="Contracts.ITemplateBindingEngine"/>.
/// <para>
/// Scriban binds this as the root object, so templates can reference:
/// <list type="bullet">
///   <item><c>{{ report.id }}</c>, <c>{{ report.name }}</c>, <c>{{ report.category }}</c></item>
///   <item><c>{{ params.invoice_no }}</c>, <c>{{ params.start_date }}</c></item>
///   <item><c>{{ data.orders }}</c>, <c>{{ data.summary }}</c></item>
///   <item><c>{{ system.now }}</c>, <c>{{ system.today }}</c>, <c>{{ system.user }}</c></item>
/// </list>
/// </para>
/// </summary>
/// <param name="Report">Metadata about the report definition being rendered.</param>
/// <param name="Params">Resolved, validated parameter values keyed by parameter name (lower-case).</param>
/// <param name="Data">Raw data result sets from data source execution, keyed by source name (lower-case).</param>
/// <param name="System">System-injected values (timestamps, user identity).</param>
public sealed record TemplateRenderContext(
    ReportMeta Report,
    IReadOnlyDictionary<string, object?> Params,
    IReadOnlyDictionary<string, object?> Data,
    SystemContext System);

/// <summary>Report definition metadata exposed to templates.</summary>
/// <param name="Id">Surrogate id of the report definition.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Category">Logical category.</param>
/// <param name="SubCategory">Optional sub-category.</param>
/// <param name="Description">Optional description.</param>
public sealed record ReportMeta(
    Guid Id,
    string Name,
    string Category,
    string? SubCategory,
    string? Description)
{
    public static ReportMeta From(ReportDefinition d) =>
        new(d.Id, d.Name, d.Category, d.SubCategory, d.Description);
}

/// <summary>System-level values injected automatically by the binding engine.</summary>
/// <param name="Now">Current UTC date-time at render time.</param>
/// <param name="Today">Current UTC date (no time component).</param>
/// <param name="User">Identity that triggered the render.</param>
public sealed record SystemContext(
    DateTimeOffset Now,
    DateOnly Today,
    string User);
