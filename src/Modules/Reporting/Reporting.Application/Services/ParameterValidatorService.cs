using System.Globalization;
using System.Text.Json;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Services;

/// <summary>
/// Pure-application-layer implementation of <see cref="IParameterValidator"/>.
/// Validates, coerces, and normalises raw caller-supplied JSON parameters against the
/// declared <see cref="ReportParameter"/> definitions.
/// </summary>
public sealed class ParameterValidatorService : IParameterValidator
{
    /// <inheritdoc/>
    public ParameterValidationResult Validate(
        IReadOnlyList<ReportParameter> declaredParameters,
        string rawParametersJson)
    {
        // For array payloads (multi-page batch), validate against the first element only.
        // All pages are expected to share the same parameter schema.
        string effectiveJson = ExtractFirstElementIfArray(rawParametersJson);

        // Parse the raw JSON into a flat string→JsonElement map
        Dictionary<string, JsonElement> raw = ParseRawJson(effectiveJson);

        var errors = new List<string>();
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (ReportParameter param in declaredParameters.OrderBy(p => p.SortOrder))
        {
            bool supplied = raw.TryGetValue(param.Name, out JsonElement element);

            // Apply default when not supplied
            if (!supplied || element.ValueKind == JsonValueKind.Null)
            {
                if (param.IsRequired)
                {
                    errors.Add($"Required parameter '{param.Name}' is missing.");
                    continue;
                }

                resolved[param.Name] = CoerceDefault(param.DefaultValue, param.ParameterType);
                continue;
            }

            // Type-coerce
            object? coerced = CoerceValue(element, param.ParameterType, param.Name, errors);

            // ValidationRuleJson check (min/max/pattern)
            if (coerced is not null && !string.IsNullOrWhiteSpace(param.ValidationRuleJson))
            {
                ValidateRule(coerced, param.ValidationRuleJson, param.Name, errors);
            }

            resolved[param.Name] = coerced;
        }

        return errors.Count > 0
            ? ParameterValidationResult.Failure(errors)
            : ParameterValidationResult.Success(resolved);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// If <paramref name="json"/> is a JSON array, returns the raw text of the first
    /// element so validation runs against a single object. Otherwise returns the input unchanged.
    /// </summary>
    private static string ExtractFirstElementIfArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array
                && doc.RootElement.GetArrayLength() > 0)
            {
                return doc.RootElement[0].GetRawText();
            }
        }
        catch (JsonException) { }

        return json;
    }

    private static Dictionary<string, JsonElement> ParseRawJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement
                .EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static object? CoerceDefault(string? defaultValue, ReportParameterType type)
    {
        if (defaultValue is null)
        {
            return null;
        }

        return type switch
        {
            ReportParameterType.WholeNumber =>
                long.TryParse(defaultValue, out long l) ? l : null,
            ReportParameterType.Numeric =>
                decimal.TryParse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) ? d : null,
            ReportParameterType.Boolean =>
                bool.TryParse(defaultValue, out bool b) ? b : null,
            ReportParameterType.Date =>
                DateOnly.TryParse(defaultValue, CultureInfo.InvariantCulture, out DateOnly date) ? date : null,
            ReportParameterType.DateTime =>
                DateTimeOffset.TryParse(defaultValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dt) ? dt : null,
            ReportParameterType.UniqueIdentifier =>
                Guid.TryParse(defaultValue, out Guid g) ? g : null,
            _ => defaultValue
        };
    }

    private static object? CoerceValue(
        JsonElement element,
        ReportParameterType type,
        string paramName,
        List<string> errors)
    {
        try
        {
            return type switch
            {
                ReportParameterType.Text or ReportParameterType.MultiValue or ReportParameterType.CascadingValue =>
                    element.ValueKind == JsonValueKind.String
                        ? element.GetString()
                        : element.GetRawText(),

                ReportParameterType.WholeNumber =>
                    element.TryGetInt64(out long l)
                        ? l
                        : long.TryParse(element.GetRawText(), out long lp)
                            ? lp
                            : Fail<long>(errors, paramName, "whole number"),

                ReportParameterType.Numeric =>
                    element.TryGetDecimal(out decimal dec)
                        ? dec
                        : decimal.TryParse(element.GetRawText(), NumberStyles.Any,
                            CultureInfo.InvariantCulture, out decimal decp)
                            ? decp
                            : Fail<decimal>(errors, paramName, "number"),

                ReportParameterType.Boolean =>
                    element.ValueKind == JsonValueKind.True ? true
                    : element.ValueKind == JsonValueKind.False ? false
                    : bool.TryParse(element.GetRawText(), out bool bp)
                        ? bp
                        : Fail<bool>(errors, paramName, "boolean"),

                ReportParameterType.Date =>
                    element.ValueKind == JsonValueKind.String &&
                    DateOnly.TryParse(element.GetString()!, CultureInfo.InvariantCulture, out DateOnly d)
                        ? d
                        : Fail<DateOnly>(errors, paramName, "date (yyyy-MM-dd)"),

                ReportParameterType.DateTime =>
                    element.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(element.GetString()!, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out DateTimeOffset dto)
                        ? dto
                        : Fail<DateTimeOffset>(errors, paramName, "datetime"),

                ReportParameterType.UniqueIdentifier =>
                    element.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(element.GetString()!, out Guid g)
                        ? g
                        : Fail<Guid>(errors, paramName, "GUID"),

                _ => element.GetRawText()
            };
        }
        catch
        {
            errors.Add($"Parameter '{paramName}' could not be coerced to expected type '{type}'.");
            return null;
        }
    }

    private static object? Fail<T>(List<string> errors, string name, string expected)
    {
        errors.Add($"Parameter '{name}' must be a valid {expected}.");
        return null;
    }

    private static void ValidateRule(
        object value,
        string ruleJson,
        string paramName,
        List<string> errors)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(ruleJson);
            JsonElement root = doc.RootElement;

            // min / max for numbers
            if (root.TryGetProperty("min", out JsonElement minEl) &&
                minEl.TryGetDouble(out double min))
            {
                double dv = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (dv < min)
                {
                    errors.Add($"Parameter '{paramName}' must be >= {min}.");
                }
            }

            if (root.TryGetProperty("max", out JsonElement maxEl) &&
                maxEl.TryGetDouble(out double max))
            {
                double dv = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (dv > max)
                {
                    errors.Add($"Parameter '{paramName}' must be <= {max}.");
                }
            }

            // pattern for strings
            if (root.TryGetProperty("pattern", out JsonElement patEl) &&
                patEl.GetString() is string pattern &&
                value is string strValue)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(strValue, pattern))
                {
                    errors.Add($"Parameter '{paramName}' does not match the required pattern '{pattern}'.");
                }
            }

            // minLength / maxLength for strings
            if (value is string sv)
            {
                if (root.TryGetProperty("minLength", out JsonElement minLenEl) &&
                    minLenEl.TryGetInt32(out int minLen) && sv.Length < minLen)
                {
                    errors.Add($"Parameter '{paramName}' must be at least {minLen} characters.");
                }

                if (root.TryGetProperty("maxLength", out JsonElement maxLenEl) &&
                    maxLenEl.TryGetInt32(out int maxLen) && sv.Length > maxLen)
                {
                    errors.Add($"Parameter '{paramName}' must not exceed {maxLen} characters.");
                }
            }
        }
        catch (JsonException)
        {
            // Malformed rule JSON — skip silently; rule was stored incorrectly at design time
        }
    }
}
