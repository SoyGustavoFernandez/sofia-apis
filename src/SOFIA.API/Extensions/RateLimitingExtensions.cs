using System.Security.Claims;
using System.Threading.RateLimiting;

namespace SOFIA.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string SignUpPolicy = "signup";
    public const string AiPolicy = "ai-endpoints";

    public static IServiceCollection AddSofiaRateLimiting(this IServiceCollection services) => services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Catch-all: 200 req/min per IP for any endpoint without a named policy
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetFixedWindowLimiter(ClientIpPartition(ctx), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

        // Credential endpoints: 10 attempts/min per IP, so one attacker cannot lock out every other client
        _ = options.AddPolicy(AuthPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientIpPartition(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

        // Public company sign-up: 5 registrations/hour per IP to curb mass tenant creation
        _ = options.AddPolicy(SignUpPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientIpPartition(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0
        }));

        // AI endpoints (prescription digitization): 10/min per company so one tenant cannot starve the others
        _ = options.AddPolicy(AiPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(TenantPartition(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        }));
    });

    public static string ClientIpPartition(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    public static string TenantPartition(HttpContext context) =>
        context.User.FindFirstValue("empresaId") is { Length: > 0 } empresaId
            ? $"tenant:{empresaId}"
            : ClientIpPartition(context);
}
