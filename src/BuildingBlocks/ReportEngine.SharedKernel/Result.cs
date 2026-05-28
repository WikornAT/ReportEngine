using System.Diagnostics.CodeAnalysis;

namespace ReportEngine.SharedKernel;

/// <summary>
/// Non-generic companion providing type-inferred factory methods for <see cref="Result{T}"/>.
/// Use <c>Result.Ok(value)</c> / <c>Result.Fail(error)</c> instead of calling static members
/// on the generic type directly (avoids CA1000).
/// </summary>
public static class Result
{
    /// <summary>Creates a successful <see cref="Result{T}"/> carrying <paramref name="value"/>.</summary>
    public static Result<T> Ok<T>(T value) => Result<T>.FromValue(value);

    /// <summary>Creates a failed <see cref="Result{T}"/> described by <paramref name="error"/>.</summary>
    public static Result<T> Fail<T>(AppError error) => Result<T>.FromError(error);
}

/// <summary>
/// A discriminated union that represents either a successful value of type <typeparamref name="T"/>
/// or a failure described by an <see cref="AppError"/>.
/// </summary>
/// <typeparam name="T">The value type carried on success.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        Error = AppError.None;
    }

    private Result(AppError error)
    {
        IsSuccess = false;
        _value = default;
        Error = error;
    }

    /// <summary><see langword="true"/> when the operation succeeded.</summary>
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess { get; }

    /// <summary><see langword="true"/> when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error that describes the failure; <see cref="AppError.None"/> on success.</summary>
    public AppError Error { get; }

    /// <summary>
    /// The result value.
    /// Accessing this on a failed result throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    // ── Internal factories (called by the non-generic Result companion) ───────

    internal static Result<T> FromValue(T value) => new(value);

    internal static Result<T> FromError(AppError error) => new(error);

    // ── Implicit conversions ──────────────────────────────────────────────────

    /// <summary>Implicitly wraps a value in a successful result.</summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Implicitly wraps an error in a failed result.</summary>
    public static implicit operator Result<T>(AppError error) => new(error);
}
