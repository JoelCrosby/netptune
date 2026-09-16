using System.Net;

using Microsoft.Extensions.Options;

namespace Netptune.ServiceDefaults.Networking;

public sealed class TrustedProxyRegistry
{
    private readonly IPNetwork[] Networks;
    private readonly bool TrustsRequestsWithoutPeerAddress;

    public TrustedProxyRegistry(IOptions<TrustedProxyOptions> options)
    {
        var configured = options.Value.Networks;
        var networks = configured.Count > 0 ? configured : [.. TrustedProxyOptions.DefaultNetworks];

        Networks = Parse(networks);
        TrustsRequestsWithoutPeerAddress = options.Value.TrustRequestsWithoutPeerAddress;
    }

    public bool IsTrusted(IPAddress? address)
    {
        if (address is null)
        {
            return TrustsRequestsWithoutPeerAddress;
        }

        var candidate = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        return Networks.Any(network => network.Contains(candidate));
    }

    public static IPNetwork[] Parse(IEnumerable<string> networks)
    {
        return
        [
            .. networks
                .Select(value => IPNetwork.TryParse(value, out var network) ? network : (IPNetwork?)null)
                .Where(network => network.HasValue)
                .Select(network => network!.Value)
        ];
    }
}
