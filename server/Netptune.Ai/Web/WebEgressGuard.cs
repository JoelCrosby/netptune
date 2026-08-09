using System.Net;
using System.Net.Sockets;

namespace Netptune.Ai.Web;

public sealed record WebEgressVerdict
{
    public required bool IsAllowed { get; init; }

    public string? Reason { get; init; }

    public static WebEgressVerdict Allowed { get; } = new() { IsAllowed = true };

    public static WebEgressVerdict Blocked(string reason)
    {
        return new WebEgressVerdict { IsAllowed = false, Reason = reason };
    }
}

public static class WebEgressGuard
{
    public static WebEgressVerdict CheckUrl(Uri uri)
    {
        var isHttp = uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        if (!isHttp)
        {
            return WebEgressVerdict.Blocked($"Only http and https URLs can be fetched, not {uri.Scheme}.");
        }

        var hasCredentials = !string.IsNullOrEmpty(uri.UserInfo);

        if (hasCredentials)
        {
            return WebEgressVerdict.Blocked("URLs carrying credentials cannot be fetched.");
        }

        var isLiteralAddress = IPAddress.TryParse(uri.Host, out var literal);

        if (isLiteralAddress)
        {
            return CheckAddress(literal!);
        }

        var isLocalName = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);

        if (isLocalName)
        {
            return WebEgressVerdict.Blocked($"{uri.Host} is not a public host.");
        }

        return WebEgressVerdict.Allowed;
    }

    public static WebEgressVerdict CheckAddress(IPAddress address)
    {
        var mapped = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        var isPrivate = IsPrivate(mapped);

        if (isPrivate)
        {
            return WebEgressVerdict.Blocked($"{address} is not a public address.");
        }

        return WebEgressVerdict.Allowed;
    }

    public static async Task<WebEgressVerdict> CheckHost(Uri uri, CancellationToken cancellationToken)
    {
        var urlVerdict = CheckUrl(uri);

        if (!urlVerdict.IsAllowed)
        {
            return urlVerdict;
        }

        var isLiteralAddress = IPAddress.TryParse(uri.Host, out _);

        if (isLiteralAddress)
        {
            return WebEgressVerdict.Allowed;
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);

            if (addresses.Length == 0)
            {
                return WebEgressVerdict.Blocked($"{uri.Host} did not resolve.");
            }

            foreach (var address in addresses)
            {
                var verdict = CheckAddress(address);

                if (!verdict.IsAllowed)
                {
                    return verdict;
                }
            }

            return WebEgressVerdict.Allowed;
        }
        catch (SocketException)
        {
            return WebEgressVerdict.Blocked($"{uri.Host} did not resolve.");
        }
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IsPrivateV6(address);
        }

        var octets = address.GetAddressBytes();

        return octets[0] switch
        {
            0 => true,
            10 => true,
            127 => true,
            100 => octets[1] >= 64 && octets[1] <= 127,
            169 => octets[1] == 254,
            172 => octets[1] >= 16 && octets[1] <= 31,
            192 => (octets[1] == 168) || (octets[1] == 0 && octets[2] == 0),
            198 => octets[1] == 18 || octets[1] == 19,
            _ => octets[0] >= 224,
        };
    }

    private static bool IsPrivateV6(IPAddress address)
    {
        var isUnspecifiedOrLocal = address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6UniqueLocal
            || address.IsIPv6Multicast
            || address.Equals(IPAddress.IPv6Any);

        if (isUnspecifiedOrLocal)
        {
            return true;
        }

        var octets = address.GetAddressBytes();
        var isNat64OrTeredo = octets[0] == 0x20 && octets[1] == 0x01 && octets[2] == 0x00;

        return isNat64OrTeredo;
    }
}
