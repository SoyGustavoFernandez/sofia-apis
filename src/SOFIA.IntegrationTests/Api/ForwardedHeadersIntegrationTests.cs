using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SOFIA.API.Extensions;

namespace SOFIA.IntegrationTests.Api;

public class ForwardedHeadersIntegrationTests
{
    private static readonly string[] AzurePrivateRanges = ["10.0.0.0/8", "100.64.0.0/10", "172.16.0.0/12", "192.168.0.0/16"];

    // Runs the forwarded headers middleware in front of an endpoint that echoes the client IP the app would see
    private static async Task<string> SeenClientIpAsync(Dictionary<string, string?> config, string peerIp, string? forwardedFor)
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddSofiaForwardedHeaders(new ConfigurationBuilder().AddInMemoryCollection(config).Build()))
                .Configure(app =>
                {
                    _ = app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse(peerIp);
                        return next();
                    });
                    _ = app.UseForwardedHeaders();
                    app.Run(context => context.Response.WriteAsync(context.Connection.RemoteIpAddress?.ToString() ?? string.Empty));
                }))
            .StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        using var response = await host.GetTestClient().SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    private static Dictionary<string, string?> NetworksConfig(params string[] networks) =>
        networks.Select((network, index) => (Key: $"{ProxyForwardingExtensions.KnownNetworksKey}:{index}", Value: (string?)network))
            .ToDictionary(x => x.Key, x => x.Value);

    [Fact]
    public async Task Middleware_ShouldIgnoreForwardedHeader_WhenNoProxyOrNetworkIsConfigured()
    {
        var seen = await SeenClientIpAsync([], "192.168.0.1", "203.0.113.5");

        _ = seen.Should().Be("192.168.0.1");
    }

    [Fact]
    public async Task Middleware_ShouldUseForwardedClient_WhenPeerIsInATrustedNetwork()
    {
        var seen = await SeenClientIpAsync(NetworksConfig(AzurePrivateRanges), "192.168.0.1", "203.0.113.5");

        _ = seen.Should().Be("203.0.113.5");
    }

    [Fact]
    public async Task Middleware_ShouldIgnoreSpoofedEntries_WhenTheTrustedHopAppendedTheRealClient()
    {
        var seen = await SeenClientIpAsync(NetworksConfig(AzurePrivateRanges), "100.100.0.7", "198.51.100.9, 203.0.113.5");

        _ = seen.Should().Be("203.0.113.5");
    }

    [Fact]
    public async Task Middleware_ShouldSkipTrustedHopsInTheChain_WhenThereAreSeveral()
    {
        var seen = await SeenClientIpAsync(NetworksConfig(AzurePrivateRanges), "192.168.0.1", "203.0.113.5, 10.1.2.3");

        _ = seen.Should().Be("203.0.113.5");
    }

    [Fact]
    public async Task Middleware_ShouldIgnoreForwardedHeader_WhenPeerIsNotTrusted()
    {
        var seen = await SeenClientIpAsync(NetworksConfig(AzurePrivateRanges), "198.51.100.7", "1.2.3.4");

        _ = seen.Should().Be("198.51.100.7");
    }

    [Fact]
    public async Task Middleware_ShouldUseForwardedClient_WhenPeerIsAKnownProxy()
    {
        var config = new Dictionary<string, string?> { [$"{ProxyForwardingExtensions.KnownProxiesKey}:0"] = "192.168.0.1" };

        var seen = await SeenClientIpAsync(config, "192.168.0.1", "203.0.113.5");

        _ = seen.Should().Be("203.0.113.5");
    }

    [Fact]
    public void AddSofiaForwardedHeaders_ShouldFailFast_WhenANetworkIsNotValid()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(NetworksConfig("not-a-network")).Build();
        using var provider = new ServiceCollection().AddSofiaForwardedHeaders(configuration).BuildServiceProvider();

        var read = () => provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        _ = read.Should().Throw<FormatException>();
    }
}
