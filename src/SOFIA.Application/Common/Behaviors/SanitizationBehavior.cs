using MediatR;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Common.Behaviors;

public class SanitizationBehavior<TRequest, TResponse>(ISanitizer sanitizer)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly HashSet<string> _skipProperties =
    [
        "password", "passwordhash", "token", "recoverytoken", "secretkey",
        "hash", "newpassword", "confirmpassword"
    ];

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        SanitizeObject(request);
        return next();
    }

    private void SanitizeObject(object obj)
    {
        var properties = obj.GetType().GetProperties()
            .Where(p => p.CanRead && p.PropertyType == typeof(string)
                        && !_skipProperties.Contains(p.Name.ToLowerInvariant()));

        foreach (var prop in properties)
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            var value = (string?)prop.GetValue(obj);
            if (value is not null)
            {
                prop.SetValue(obj, sanitizer.Sanitize(value));
            }
        }
    }
}
