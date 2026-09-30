using System.Net;
using System.Text.Json;
using SOFIA.API.Infrastructure;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Security;

public class CsrfHeaderIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // The test host runs as Development, whose CORS policy allows the local SPA
    private const string AllowedOrigin = "http://localhost:4200";

    [Theory]
    [InlineData("/api/v1/auth/refresh")]
    [InlineData("/api/v1/auth/logout")]
    public async Task CookieEndpoint_ShouldReturn403WithCsrfCode_WhenHeaderIsMissing(string url)
    {
        var client = CreateClient();

        var response = await client.PostAsync(url, content: null);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(RequireCsrfHeaderAttribute.ErrorCode);
    }

    [Fact]
    public async Task Refresh_ShouldReachTheAction_WhenHeaderIsPresent()
    {
        var response = await PostWithHeaderAsync("/api/v1/auth/refresh");

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: "without a refresh cookie the action itself rejects the call");
    }

    [Fact]
    public async Task Logout_ShouldSucceed_WhenHeaderIsPresent()
    {
        var response = await PostWithHeaderAsync("/api/v1/auth/logout");

        _ = response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Preflight_ShouldAllowCsrfHeader_OnlyForConfiguredOrigins()
    {
        var client = CreateClient();

        var allowed = await client.SendAsync(Preflight(AllowedOrigin));
        var foreign = await client.SendAsync(Preflight("https://evil.example"));

        _ = allowed.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(AllowedOrigin);
        _ = allowed.Headers.GetValues("Access-Control-Allow-Headers").Single().Should().ContainEquivalentOf(RequireCsrfHeaderAttribute.HeaderName);
        _ = foreign.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse(because: "a foreign site must not be able to send the anti-CSRF header");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private HttpClient CreateClient() => Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private async Task<HttpResponseMessage> PostWithHeaderAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(RequireCsrfHeaderAttribute.HeaderName, "1");
        return await CreateClient().SendAsync(request);
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", RequireCsrfHeaderAttribute.HeaderName.ToLowerInvariant());
        return request;
    }
}
