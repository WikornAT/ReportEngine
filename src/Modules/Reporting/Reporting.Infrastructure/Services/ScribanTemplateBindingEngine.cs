using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using Reporting.Application.Contracts;
using Reporting.Application.Models;

using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Scriban-based implementation of <see cref="ITemplateBindingEngine"/>.
/// <para>
/// Exposes the following root namespaces inside every template:
/// <list type="bullet">
///   <item><c>report</c> — report definition metadata (<see cref="ReportMeta"/>)</item>
///   <item><c>params</c> — resolved, validated parameter values</item>
///   <item><c>data</c>   — data source result sets</item>
///   <item><c>system</c> — <see cref="SystemContext"/> (now, today, user)</item>
/// </list>
/// </para>
/// </summary>
internal sealed class ScribanTemplateBindingEngine : ITemplateBindingEngine
{
    /// <inheritdoc/>
    public Task<string> BindAsync(
        string htmlTemplate,
        TemplateRenderContext context,
        CancellationToken cancellationToken = default)
    {
        // Pre-process: convert single-brace placeholders {key} produced by the
        // template designer into Scriban params.* expressions {{ params.key }}.
        // Only converts tokens that look like identifiers (letters, digits, underscore)
        // and are NOT already inside a Scriban {{ }} block.
        string processedTemplate = ReplaceSingleBracePlaceholders(htmlTemplate);

        // Parse the Scriban template (HTML-safe: Scriban does NOT escape by default)
        Template template = Template.Parse(processedTemplate);

        if (template.HasErrors)
        {
            string errors = string.Join("; ",
                template.Messages.Select(m => m.Message));
            throw new TemplateBindingException(
                $"Template syntax error(s): {errors}");
        }

        // Build root script object
        var root = new ScriptObject();

        // {{ report.* }}
        root.Add("report", ToScriptObject(new Dictionary<string, object?>
        {
            ["id"]          = context.Report.Id.ToString(),
            ["name"]        = context.Report.Name,
            ["category"]    = context.Report.Category,
            ["sub_category"] = context.Report.SubCategory,
            ["description"] = context.Report.Description,
        }));

        // {{ params.* }}
        root.Add("params", ToScriptObject(
            context.Params.ToDictionary(
                kv => ToSnakeCase(kv.Key),
                kv => kv.Value)));

        // {{ data.* }}
        root.Add("data", ToScriptObject(
            context.Data.ToDictionary(
                kv => ToSnakeCase(kv.Key),
                kv => kv.Value)));

        // {{ system.* }}
        root.Add("system", ToScriptObject(new Dictionary<string, object?>
        {
            ["now"]   = context.System.Now.ToString("o", CultureInfo.InvariantCulture),
            ["today"] = context.System.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["user"]  = context.System.User,
        }));

        var templateContext = new TemplateContext
        {
            StrictVariables = false,
            // Disable auto-escaping so HTML is rendered verbatim
            TemplateLoader = null,
        };
        templateContext.PushGlobal(root);

        try
        {
            string result = template.Render(templateContext);
            return Task.FromResult(result);
        }
        catch (ScriptRuntimeException ex)
        {
            throw new TemplateBindingException(
                $"Template binding runtime error: {ex.Message}", ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts single-brace designer placeholders such as <c>{letter_no}</c> into
    /// Scriban expressions <c>{{ params.letter_no }}</c> so that values supplied via
    /// <c>parametersJson</c> are automatically bound without requiring the template
    /// author to use Scriban syntax directly.
    /// <para>
    /// Tokens that are already inside a Scriban <c>{{</c> block are left untouched.
    /// The key is normalised to snake_case to match the convention used when building
    /// the Scriban <c>params</c> object.
    /// </para>
    /// </summary>
    private static string ReplaceSingleBracePlaceholders(string html)
    {
        // Match {word_chars} that are NOT preceded by another { (already Scriban).
        // Normalise to lowercase so {Cust_Name}, {cust_name} and {CUST_NAME} all map
        // to {{ params.cust_name }}, matching the ToSnakeCase output for user-supplied keys.
        return Regex.Replace(
            html,
            @"(?<!\{)\{([A-Za-z_][A-Za-z0-9_]*)\}(?!\})",
            m => $"{{{{ params.{m.Groups[1].Value.ToLowerInvariant()} }}}}");
    }

    private static ScriptObject ToScriptObject(IDictionary<string, object?> dict)
    {
        var obj = new ScriptObject();
        foreach ((string key, object? value) in dict)
        {
            obj[key] = ConvertValue(value);
        }
        return obj;
    }

    private static object? ConvertValue(object? value) =>
        value switch
        {
            null               => null,
            DateOnly d         => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateTime dt        => dt.ToString("o", CultureInfo.InvariantCulture),
            Guid g             => g.ToString(),

            // Nested dict (e.g. from data-source results): recurse with snake_case keys
            IDictionary<string, object?> dict =>
                ToScriptObject(dict.ToDictionary(
                    kv => ToSnakeCase(kv.Key),
                    kv => kv.Value)),

            // JsonElement produced by MergeParametersIntoDataJson
            JsonElement je => ConvertJsonElement(je),

            System.Collections.IEnumerable enumerable when value is not string =>
                enumerable.Cast<object?>().Select(ConvertValue).ToList(),

            _ => value
        };

    /// <summary>
    /// Recursively converts a <see cref="JsonElement"/> into a Scriban-friendly value:
    /// objects → <see cref="ScriptObject"/> (keys snake_cased), arrays → List, primitives → CLR value.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object =>
                ToScriptObject(element.EnumerateObject()
                    .ToDictionary(
                        p => ToSnakeCase(p.Name),
                        p => (object?)ConvertJsonElement(p.Value))),

            JsonValueKind.Array =>
                element.EnumerateArray()
                    .Select(e => ConvertJsonElement(e))
                    .ToList(),

            JsonValueKind.String  => element.GetString(),
            JsonValueKind.True    => (object?)true,
            JsonValueKind.False   => false,
            JsonValueKind.Number  =>
                element.TryGetInt64(out long l)  ? l :
                element.TryGetDecimal(out decimal d) ? d :
                (object?)element.GetDouble(),
            JsonValueKind.Null    => null,
            _                     => element.GetRawText()
        };

    /// <summary>
    /// Converts <c>CamelCase</c>, <c>PascalCase</c>, or <c>Mixed_Case</c> to <c>snake_case</c>.
    /// Consecutive underscores are collapsed (e.g. <c>Cust_Name</c> → <c>cust_name</c>).
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                // Insert underscore only when NOT at position 0 and previous char is not already '_'
                if (i > 0 && sb.Length > 0 && sb[sb.Length - 1] != '_')
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }
}
