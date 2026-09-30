using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Api;

public class SecurityHeadersIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task NotFoundResponse_ShouldCarrySecurityAndNoStoreHeaders()
    {
        var response = await CreateClient().GetAsync("/api/v1/test-errors/missing");

        _ = response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task UnauthorizedResponse_ShouldCarrySecurityAndNoStoreHeaders()
    {
        var response = await CreateClient().GetAsync("/api/v1/ruta-que-no-existe");

        _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task ServerErrorResponse_ShouldKeepSecurityAndNoStoreHeaders_WhenExceptionHandlerRewritesIt()
    {
        var response = await CreateClient().GetAsync("/api/v1/test-errors/boom");

        _ = response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldExposeOnlyStatusAndTimestamp()
    {
        var response = await CreateClient().GetAsync("/health");

        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _ = body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["status", "timestamp"]);
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        _ = response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        _ = response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
        _ = response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    // The throwing controller lives in the test assembly, so it is only mounted on this host
    private HttpClient CreateClient() => Factory
        .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly)))
        .CreateClient(new() { BaseAddress = new Uri("https://localhost") });
}

[ApiController]
[AllowAnonymous]
[Route("api/v1/test-errors")]
public class ThrowingTestController : ControllerBase
{
    [HttpGet("missing")]
    public IActionResult Missing() => NotFound();

    [HttpGet("boom")]
    public IActionResult Boom() => throw new InvalidOperationException("boom");
}
