using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Netptune.ServiceDefaults.Networking;

using Xunit;

namespace Netptune.UnitTests.Netptune.ServiceDefaults.Networking;

public class ClientIpAddressMiddlewareTests
{
    private const string ClusterPeer = "10.1.2.3";
    private const string PublicPeer = "203.0.113.9";
    private const string ClaimedClient = "198.51.100.7";
    private const string EdgeHeader = "CF-Connecting-IP";
    private const string EdgeAuthHeader = "X-Netptune-Edge";
    private const string EdgeSecret = "edge-secret-value";

    [Fact]
    public async Task Resolve_ShouldUseCloudflareHeader_WhenPeerIsTrusted()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context);

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    [Fact]
    public async Task Resolve_ShouldUseForwardedFor_WhenPeerIsTrustedAndCloudflareHeaderAbsent()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers["X-Forwarded-For"] = $"{ClaimedClient}, 10.1.2.4";

        await Invoke(context);

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreCloudflareHeader_WhenPeerIsNotTrusted()
    {
        var context = CreateContext(PublicPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context);

        context.GetClientIpAddress().Should().Be(PublicPeer);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreForwardedFor_WhenPeerIsNotTrusted()
    {
        var context = CreateContext(PublicPeer);
        context.Request.Headers["X-Forwarded-For"] = ClaimedClient;

        await Invoke(context);

        context.GetClientIpAddress().Should().Be(PublicPeer);
    }

    [Fact]
    public async Task Resolve_ShouldFallBackToPeer_WhenTrustedPeerSendsNoForwardingHeaders()
    {
        var context = CreateContext(ClusterPeer);

        await Invoke(context);

        context.GetClientIpAddress().Should().Be(ClusterPeer);
    }

    [Fact]
    public async Task Resolve_ShouldTreatMappedAddressesAsTrusted_WhenPeerIsIPv4MappedToIPv6()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ClusterPeer).MapToIPv6();
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context);

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreForwardingHeaders_WhenThereIsNoPeerAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context);

        context.GetClientIpAddress().Should().BeNull();
    }

    [Fact]
    public async Task Resolve_ShouldHonourForwardingHeaders_WhenPeerlessRequestsAreExplicitlyTrusted()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context, trustRequestsWithoutPeerAddress: true);

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreTheEdgeHeader_WhenNoClientAddressHeaderIsConfigured()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context, clientAddressHeader: null);

        context.GetClientIpAddress().Should().Be(ClusterPeer);
    }

    [Fact]
    public async Task Resolve_ShouldReadWhicheverHeaderIsConfigured_WhenTheEdgeIsNotCloudflare()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers["True-Client-IP"] = ClaimedClient;

        await Invoke(context, clientAddressHeader: "True-Client-IP");

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    [Fact]
    public async Task Resolve_ShouldStillUseForwardedFor_WhenNoClientAddressHeaderIsConfigured()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers["X-Forwarded-For"] = $"{ClaimedClient}, 10.1.2.4";

        await Invoke(context, clientAddressHeader: null);

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    [Fact]
    public async Task Resolve_ShouldHonourTheAddressHeader_WhenTheEdgeSecretMatches()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;
        context.Request.Headers[EdgeAuthHeader] = EdgeSecret;

        await Invoke(context, edgeAuthorizationSecret: EdgeSecret);

        context.GetClientIpAddress().Should().Be(ClaimedClient);
    }

    // The direct-to-origin case: the peer is the ingress and therefore trusted, but the caller never
    // passed the edge and so cannot present its secret.
    [Fact]
    public async Task Resolve_ShouldIgnoreTheAddressHeader_WhenTheEdgeSecretIsAbsent()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;

        await Invoke(context, edgeAuthorizationSecret: EdgeSecret);

        context.GetClientIpAddress().Should().Be(ClusterPeer);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreTheAddressHeader_WhenTheEdgeSecretIsWrong()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;
        context.Request.Headers[EdgeAuthHeader] = "not-the-secret";

        await Invoke(context, edgeAuthorizationSecret: EdgeSecret);

        context.GetClientIpAddress().Should().Be(ClusterPeer);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreForwardedFor_WhenTheEdgeSecretIsAbsent()
    {
        var context = CreateContext(ClusterPeer);
        context.Request.Headers["X-Forwarded-For"] = ClaimedClient;

        await Invoke(context, edgeAuthorizationSecret: EdgeSecret);

        context.GetClientIpAddress().Should().Be(ClusterPeer);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreTheAddressHeader_WhenTheSecretIsPresentedByAnUntrustedPeer()
    {
        var context = CreateContext(PublicPeer);
        context.Request.Headers[EdgeHeader] = ClaimedClient;
        context.Request.Headers[EdgeAuthHeader] = EdgeSecret;

        await Invoke(context, edgeAuthorizationSecret: EdgeSecret);

        context.GetClientIpAddress().Should().Be(PublicPeer);
    }

    private static DefaultHttpContext CreateContext(string peer)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);

        return context;
    }

    private static Task Invoke(
        HttpContext context,
        bool trustRequestsWithoutPeerAddress = false,
        string? clientAddressHeader = EdgeHeader,
        string? edgeAuthorizationSecret = null)
    {
        var options = new TrustedProxyOptions
        {
            TrustRequestsWithoutPeerAddress = trustRequestsWithoutPeerAddress,
            ClientAddressHeader = clientAddressHeader,
            EdgeAuthorizationHeader = edgeAuthorizationSecret is null ? null : EdgeAuthHeader,
            EdgeAuthorizationSecret = edgeAuthorizationSecret,
        };
        var monitor = new StaticOptionsMonitor<TrustedProxyOptions>(options);
        var registry = new TrustedProxyRegistry(Options.Create(options));
        var middleware = new ClientIpAddressMiddleware(_ => Task.CompletedTask);

        return middleware.InvokeAsync(context, registry, monitor);
    }

    private sealed class StaticOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        public StaticOptionsMonitor(TOptions value)
        {
            CurrentValue = value;
        }

        public TOptions CurrentValue { get; }

        public TOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<TOptions, string?> listener)
        {
            return null;
        }
    }
}
