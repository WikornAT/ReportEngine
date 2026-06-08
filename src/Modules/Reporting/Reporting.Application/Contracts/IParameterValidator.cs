using Reporting.Domain.ReportDefinitions;

namespace Reporting.Application.Contracts;

/// <summary>
/// Contract for validating, type-coercing, and normalising the raw parameter JSON
/// supplied by a caller against the declared <see cref="ReportParameter"/> definitions.
/// </summary>
public interface IParameterValidator
{
    /// <summary>
    /// Validates <paramref name="rawParametersJson"/> against <paramref name="declaredParameters"/>.
    /// <list type="bullet">
    ///   <item>Missing required parameters are reported as errors.</item>
    ///   <item>Values are coerced / normalised to their declared <see cref="Domain.Enums.ReportParameterType"/>.</item>
    ///   <item>Values that violate <see cref="ReportParameter.ValidationRuleJson"/> are reported as errors.</item>
    ///   <item>Missing optional parameters are filled from <see cref="ReportParameter.DefaultValue"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="declaredParameters">Ordered list of declared parameters from the report definition.</param>
    /// <param name="rawParametersJson">JSON object supplied by the caller, e.g. <c>{"invoiceNo":"INV-001"}</c>.</param>
    /// <returns>
    /// A <see cref="ParameterValidationResult"/> carrying either the resolved value dictionary
    /// or a list of validation errors.
    /// </returns>
    ParameterValidationResult Validate(
        IReadOnlyList<ReportParameter> declaredParameters,
        string rawParametersJson);
}

/// <summary>
/// Result of parameter validation.
/// </summary>
public sealed class ParameterValidationResult
{
    private ParameterValidationResult() { }

    /// <summary><see langword="true"/> when validation passed with no errors.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Resolved, normalised parameter values keyed by parameter name (original casing).
    /// Only populated when <see cref="IsValid"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Values { get; private init; } =
        new Dictionary<string, object?>();

    /// <summary>Validation error messages. Empty when <see cref="IsValid"/> is <see langword="true"/>.</summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>Creates a successful result with the resolved values.</summary>
    public static ParameterValidationResult Success(IReadOnlyDictionary<string, object?> values) =>
        new() { Values = values, Errors = [] };

    /// <summary>Creates a failed result with one or more error messages.</summary>
    public static ParameterValidationResult Failure(IReadOnlyList<string> errors) =>
        new() { Errors = errors };
}
