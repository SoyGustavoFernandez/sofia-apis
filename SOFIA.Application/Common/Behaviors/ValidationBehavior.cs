using FluentValidation;
using MediatR;
using SOFIA.Domain.Common;
namespace SOFIA.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethods()
                    .First(m => m.Name == "Failure" && m.IsGenericMethod)
                    .MakeGenericMethod(resultType);

                var error = Error.Validation("ValidationError", string.Join("; ", failures.Select(f => f.ErrorMessage)));
                return (TResponse)failureMethod.Invoke(null, [error, 400])!;
            }

            if (typeof(TResponse) == typeof(Result))
            {
                var error = Error.Validation("ValidationError", string.Join("; ", failures.Select(f => f.ErrorMessage)));
                return (TResponse)(object)Result.Failure(error, 400);
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
