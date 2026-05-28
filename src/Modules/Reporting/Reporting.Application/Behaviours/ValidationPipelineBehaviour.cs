using FluentValidation;
using FluentValidation.Results;

using MediatR;

using ReportEngine.SharedKernel;

namespace Reporting.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered
/// <see cref="IValidator{T}"/> instances for a request before the handler executes.
/// <para>
/// When one or more validation rules fail, the behaviour short-circuits the pipeline
/// and returns a <see cref="Result{T}"/> containing a <see cref="AppError.Validation"/> error
/// whose message lists all failures — without throwing an exception.
/// </para>
/// <para>
/// Registered automatically via <c>AddReportingApplication()</c>. No attribute decoration
/// or handler changes are needed.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type — must be <c>Result&lt;T&gt;</c>.</typeparam>
public sealed class ValidationPipelineBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        string message = string.Join(" | ", failures.Select(f => f.ErrorMessage));

        // Build a Result<T> failure via reflection so the behaviour stays generic.
        // TResponse is always Result<T> for our handlers.
        Type responseType = typeof(TResponse);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            AppError error = AppError.Validation(message);
            Type valueType = responseType.GetGenericArguments()[0];
            object result = typeof(Result)
                .GetMethod(nameof(Result.Fail))!
                .MakeGenericMethod(valueType)
                .Invoke(null, [error])!;

            return (TResponse)result;
        }

        // Fallback — should not happen if all handlers use Result<T>.
        throw new ValidationException(failures);
    }
}
