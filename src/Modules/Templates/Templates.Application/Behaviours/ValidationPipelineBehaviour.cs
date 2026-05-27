using FluentValidation;
using FluentValidation.Results;

using MediatR;

using Exim.ReportEngine.SharedKernel;

namespace Templates.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered <see cref="IValidator{T}"/> instances
/// for a request before the handler executes. Short-circuits with a validation error Result
/// when any rule fails.
/// </summary>
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

        throw new ValidationException(failures);
    }
}
