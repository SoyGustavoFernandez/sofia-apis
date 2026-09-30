using Microsoft.AspNetCore.Mvc.Filters;

namespace SOFIA.API.Infrastructure;

// Cookie-authenticated endpoints: a cross-site form cannot set custom headers and a cross-origin fetch with one needs a CORS preflight
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireCsrfHeaderAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-SOFIA-CSRF";
    public const string ErrorCode = "Auth.Csrf";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.HttpContext.Request.Headers[HeaderName]))
        {
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Detail = $"The {HeaderName} header is required.",
        };
        problemDetails.Extensions["code"] = ErrorCode;

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status403Forbidden,
            ContentTypes = { "application/problem+json" },
        };
    }
}
