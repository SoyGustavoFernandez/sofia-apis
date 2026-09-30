using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.API.Extensions;
using SOFIA.API.Infrastructure;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Security;

public class RateLimitingIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string ClientIpHeader = "X-Test-Client-Ip";
    private const int AuthPermitLimit = 10;

    [Fact]
    public async Task Login_ShouldReturn429_WhenSameIpExceedsAuthLimit()
    {
        var client = CreateClient();

        await ExhaustLoginQuotaAsync(client, "10.0.0.1");
        var response = await PostLoginAsync(client, "10.0.0.1");

        _ = response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_ShouldNotThrottleOtherIp_WhenOneIpExhaustedAuthLimit()
    {
        var client = CreateClient();

        await ExhaustLoginQuotaAsync(client, "10.0.0.2");
        var response = await PostLoginAsync(client, "10.0.0.3");

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: "the auth quota is per client IP, not global");
    }

    [Fact]
    public async Task Refresh_ShouldNotBeThrottled_WhenLoginQuotaIsExhausted()
    {
        var client = CreateClient();

        await ExhaustLoginQuotaAsync(client, "10.0.0.4");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add(ClientIpHeader, "10.0.0.4");
        request.Headers.Add(RequireCsrfHeaderAttribute.HeaderName, "1");
        var response = await client.SendAsync(request);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: "session refresh must not share the credential brute-force quota");
    }

    [Fact]
    public void TenantPartition_ShouldIsolateCompanies_WhenUsersShareTheSameIp()
    {
        var empresaA = ContextFor("192.168.1.10", Guid.NewGuid());
        var empresaB = ContextFor("192.168.1.10", Guid.NewGuid());

        _ = RateLimitingExtensions.TenantPartition(empresaA)
            .Should().NotBe(RateLimitingExtensions.TenantPartition(empresaB));
    }

    [Fact]
    public void TenantPartition_ShouldShareQuota_WhenUsersBelongToTheSameCompany()
    {
        var empresaId = Guid.NewGuid();

        _ = RateLimitingExtensions.TenantPartition(ContextFor("192.168.1.10", empresaId))
            .Should().Be(RateLimitingExtensions.TenantPartition(ContextFor("192.168.1.99", empresaId)));
    }

    [Fact]
    public void TenantPartition_ShouldFallBackToClientIp_WhenUserHasNoCompany()
    {
        var context = ContextFor("192.168.1.10", empresaId: null);

        _ = RateLimitingExtensions.TenantPartition(context)
            .Should().Be(RateLimitingExtensions.ClientIpPartition(context));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    // TestServer has no real socket, so the client IP is injected per request before the rate limiter runs
    private HttpClient CreateClient() => Factory
        .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter, ClientIpStartupFilter>()))
        .CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static async Task ExhaustLoginQuotaAsync(HttpClient client, string ip)
    {
        for (var i = 0; i < AuthPermitLimit; i++)
        {
            var response = await PostLoginAsync(client, ip);
            _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: "attempts within the quota must reach the handler");
        }
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string ip)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { nombreUsuario = $"nadie_{ip}", password = "ClaveIncorrecta1" }),
        };
        request.Headers.Add(ClientIpHeader, ip);
        return await client.SendAsync(request);
    }

    private static DefaultHttpContext ContextFor(string ip, Guid? empresaId)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (empresaId is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("empresaId", empresaId.Value.ToString())], "Test"));
        }

        return context;
    }

    private sealed class ClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            _ = app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                }

                await nextMiddleware();
            });
            next(app);
        };
    }
}
