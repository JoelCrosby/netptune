using Microsoft.Extensions.Options;

namespace Netptune.ServiceDefaults.Networking;

// A half-configured edge check does not fail visibly at runtime -- it quietly stops believing the
// address header, and every caller collapses into the ingress's own address for rate limiting. That
// reads as a rate limiting bug days later, so it is refused at startup instead.
public sealed class TrustedProxyOptionsValidator : IValidateOptions<TrustedProxyOptions>
{
    public ValidateOptionsResult Validate(string? name, TrustedProxyOptions options)
    {
        var failures = new List<string>();

        if (options.RequiresEdgeAuthorization && string.IsNullOrWhiteSpace(options.EdgeAuthorizationHeader))
        {
            failures.Add(
                $"{TrustedProxyOptions.SectionName}:EdgeAuthorizationSecret is set but "
                + $"{TrustedProxyOptions.SectionName}:EdgeAuthorizationHeader is not, so the secret can never be presented.");
        }

        if (!options.RequiresEdgeAuthorization && !string.IsNullOrWhiteSpace(options.EdgeAuthorizationHeader))
        {
            failures.Add(
                $"{TrustedProxyOptions.SectionName}:EdgeAuthorizationHeader is set but "
                + $"{TrustedProxyOptions.SectionName}:EdgeAuthorizationSecret is empty, so the header would not be checked.");
        }

        var invalidNetworks = options.Networks
            .Where(network => !System.Net.IPNetwork.TryParse(network, out _))
            .ToList();

        if (invalidNetworks.Count > 0)
        {
            failures.Add(
                $"{TrustedProxyOptions.SectionName}:Networks contains values that are not CIDR ranges: "
                + string.Join(", ", invalidNetworks));
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
