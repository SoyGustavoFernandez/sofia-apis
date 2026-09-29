using SOFIA.Application.Security;

namespace SOFIA.API.Infrastructure;

// Anonymous endpoints (login, refresh, logout, recovery) stay open; authenticated ones must opt in explicitly
public class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is not null &&
            context.User.HasClaim(PasswordChangePolicy.ClaimType, "true") &&
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null &&
            endpoint.Metadata.GetMetadata<AllowDuringPasswordChangeAttribute>() is null)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Detail = PasswordChangePolicy.RequiredError.Message,
            };
            problemDetails.Extensions["code"] = PasswordChangePolicy.RequiredError.Code;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
            return;
        }

        await next(context);
    }
}
