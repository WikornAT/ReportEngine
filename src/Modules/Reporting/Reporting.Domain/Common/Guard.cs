namespace Reporting.Domain.Common;

/// <summary>
/// Lightweight guard-clause helper for domain argument validation.
/// Keeps domain constructors and factory methods concise and intention-revealing.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">Reference or nullable value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Parameter name, forwarded to the exception (use <c>nameof</c>).</param>
    /// <returns>The non-null <paramref name="value"/>.</returns>
    public static T NotNull<T>(T? value, string paramName)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ReportingDomainException"/> when <paramref name="value"/> is
    /// <see langword="null"/>, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="fieldName">Human-readable field name used in the exception message.</param>
    /// <returns>The validated, non-empty <paramref name="value"/>.</returns>
    public static string NotNullOrWhiteSpace(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ReportingDomainException($"'{fieldName}' must not be null or empty.");
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ReportingDomainException"/> when <paramref name="value"/> is
    /// less than or equal to zero.
    /// </summary>
    /// <param name="value">The integer to check.</param>
    /// <param name="fieldName">Human-readable field name used in the exception message.</param>
    /// <returns>The validated positive <paramref name="value"/>.</returns>
    public static int Positive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ReportingDomainException($"'{fieldName}' must be a positive integer.");
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ReportingDomainException"/> when <paramref name="value"/> does not
    /// fall within the defined values of enum type <typeparamref name="TEnum"/>.
    /// </summary>
    /// <typeparam name="TEnum">An enum type.</typeparam>
    /// <param name="value">The enum value to check.</param>
    /// <param name="fieldName">Human-readable field name used in the exception message.</param>
    /// <returns>The validated <paramref name="value"/>.</returns>
    public static TEnum DefinedEnum<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ReportingDomainException($"'{fieldName}' has an undefined enum value '{value}'.");
        }

        return value;
    }
}
