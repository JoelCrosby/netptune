namespace Netptune.ServiceDefaults.Networking;

public sealed class TrustedProxyOptions
{
    public const string SectionName = "TrustedProxies";

    // Every host runs behind an in-cluster ingress, so the defaults cover the private ranges a
    // sidecar or ingress controller can reach us from. A request that arrives from anywhere else
    // reached the pod directly and its forwarding headers are not evidence of anything.
    public static readonly string[] DefaultNetworks =
    [
        "127.0.0.0/8",
        "::1/128",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
        "fc00::/7",
    ];

    // Left empty so configuration replaces the defaults rather than appending to them; the
    // registry substitutes DefaultNetworks when nothing is configured.
    public List<string> Networks { get; set; } = [];

    // An in-process host has no peer address, so nothing can be verified about where a request came
    // from. Off here because a real deployment always has a peer; the integration tests turn it on so
    // they can give each case its own forwarded address.
    public bool TrustRequestsWithoutPeerAddress { get; set; }

    // A CDN that terminates TLS usually carries the caller's address in a header of its own naming --
    // Cloudflare calls it CF-Connecting-IP, others differ. Naming it here rather than in code keeps
    // the application portable between edges, and keeps the trust decision reviewable in one place.
    // Empty means only the standard X-Forwarded-For chain is read.
    public string? ClientAddressHeader { get; set; }

    // ClientAddressHeader is only evidence of anything while the edge is the only party who can set
    // it, and an origin with a public address is reachable without going through the edge at all.
    // Rather than fence the origin off by network address -- a list that has to be maintained or the
    // site stops working -- the edge stamps a secret that only it knows, and the address header is
    // believed only when that secret is present. Nothing here expires or drifts.
    //
    // Leave the secret empty to skip the check, which is what local development and the tests do.
    public string? EdgeAuthorizationHeader { get; set; }

    public string? EdgeAuthorizationSecret { get; set; }

    public bool RequiresEdgeAuthorization => !string.IsNullOrWhiteSpace(EdgeAuthorizationSecret);
}
