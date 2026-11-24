using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace SOFIA.API.Extensions;

public static class ProxyForwardingExtensions
{
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";
    public const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";

    // X-Forwarded-* is honored only from the proxies and networks listed in config, never from arbitrary clients
    public static IServiceCollection AddSofiaForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var knownProxies = configuration.GetSection(KnownProxiesKey).Get<string[]>() ?? [];
        var knownNetworks = configuration.GetSection(KnownNetworksKey).Get<string[]>() ?? [];

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Empty KnownProxies/KnownIPNetworks makes the middleware trust every client, so no trusted peers means no forwarding
            options.ForwardedHeaders = knownProxies.Length == 0 && knownNetworks.Length == 0
                ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            // Walk the whole chain: addresses added by trusted hops are skipped and the first untrusted one is the client
            options.ForwardLimit = null;

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });
    }
}
