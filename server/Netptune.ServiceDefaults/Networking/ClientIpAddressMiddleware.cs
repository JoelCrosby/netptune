using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Netptune.ServiceDefaults.Networking;

// Runs before UseForwardedHeaders, while Connection.RemoteIpAddress is still the peer that actually
// opened the socket. Only that ordering can tell a forwarded address apart from a claimed one.
public sealed class ClientIpAddressMiddleware(RequestDelegate next)
{
    internal const string ItemKey = "netptune.client-ip";

    private const string ForwardedForHeader = "X-Forwarded-For";

    public Task InvokeAsync(
        HttpContext context,
        TrustedProxyRegistry trustedProxies,
        IOptionsMonitor<TrustedProxyOptions> options)
    {
        var peer = context.Connection.RemoteIpAddress;

        context.Items[ItemKey] = Resolve(context, peer, trustedProxies, options.CurrentValue);

        return next(context);
    }

    private static string? Resolve(
        HttpContext context,
        IPAddress? peer,
        TrustedProxyRegistry trustedProxies,
        TrustedProxyOptions options)
    {
        var peerAddress = peer?.ToString();

        if (!trustedProxies.IsTrusted(peer))
        {
            return peerAddress;
        }

        // A request that reached the origin without passing the edge can set any address it likes, so
        // the address headers are only read once the edge has identified itself.
        if (!EdgeAuthorization.IsSatisfied(context, options))
        {
            return peerAddress;
        }

        var forwardedAddress = ReadClientAddressHeader(context, options.ClientAddressHeader);

        if (forwardedAddress is not null)
        {
            return forwardedAddress;
        }

        var forwardedFor = context.Request.Headers[ForwardedForHeader].FirstOrDefault();
        var originatingAddress = forwardedFor?.Split(',').FirstOrDefault()?.Trim();

        return string.IsNullOrWhiteSpace(originatingAddress) ? peerAddress : originatingAddress;
    }

    // The edge in front of us decides this header's name, so it is configuration rather than a
    // constant. Unset means the standard forwarded chain is the only thing we read.
    private static string? ReadClientAddressHeader(HttpContext context, string? clientAddressHeader)
    {
        if (string.IsNullOrWhiteSpace(clientAddressHeader))
        {
            return null;
        }

        var value = context.Request.Headers[clientAddressHeader].FirstOrDefault();

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
