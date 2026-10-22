using System.Text.RegularExpressions;

namespace Netptune.AppHost;

// Mirrors charts/netptune/values.yaml, with the hosts moved under .localhost, which browsers resolve to
// loopback and treat as a secure context, so the Secure auth cookies still work over plain http.
public static partial class ClusterParity
{
    public const string ConfigurationKey = "Netptune:ClusterParity";

    public const int EdgePort = 8000;

    public const int AppPort = 7400;

    public const int ApiPort = 7600;

    public const string RootHostName = "netptune.localhost";

    public const string AppHostName = "app.netptune.localhost";

    public const string ApiHostName = "api.netptune.localhost";

    public const string WebsiteHostName = "www.netptune.localhost";

    public const string ClientAddressHeader = "CF-Connecting-IP";

    public const string EdgeAuthorizationHeader = "X-Netptune-Edge";

    // Cloudflare's always-passes test secret, paired with the test site key in the client's localhost build.
    public const string TurnstileTestSecretKey = "1x0000000000000000000000000000000AA";

    public static readonly string[] TrustedNetworks =
    [
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
        "127.0.0.0/8",
        "::1/128",
        "fc00::/7",
    ];

    public static string AppOrigin => $"http://{AppHostName}:{EdgePort}";

    public static bool IsEnabled(IDistributedApplicationBuilder builder)
    {
        var configured = builder.Configuration[ConfigurationKey];

        return bool.TryParse(configured, out var enabled) && enabled;
    }

    // Read from the chart rather than computed from index.html, so a hash that has drifted fails here the
    // same way it fails in the cluster.
    public static string ReadCspInlineScriptHash(IDistributedApplicationBuilder builder)
    {
        var valuesPath = Path.Combine(builder.AppHostDirectory, "..", "..", "charts", "netptune", "values.yaml");
        var values = File.ReadAllText(valuesPath);
        var match = InlineScriptHashPattern().Match(values);

        if (!match.Success)
        {
            throw new InvalidOperationException($"csp.inlineScriptHash was not found in {valuesPath}.");
        }

        return match.Groups["hash"].Value;
    }

    [GeneratedRegex("""inlineScriptHash:\s*"(?<hash>[^"]+)"\s*$""", RegexOptions.Multiline)]
    private static partial Regex InlineScriptHashPattern();
}
