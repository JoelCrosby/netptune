using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Netptune.Identity.Authentication;
using Netptune.Identity.Authorization.Requirements;

namespace Netptune.Identity.Authorization;

public class NetptuneAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly string _authenticationScheme;

    public NetptuneAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IOptions<NetptuneAuthorizationOptions> netptuneOptions)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
        _authenticationScheme = netptuneOptions.Value.AuthenticationScheme;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await _fallback.GetPolicyAsync(policyName);

        if (policy is not null)
        {
            return policy;
        }

        return new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(_authenticationScheme)
            .AddRequirements(new WorkspaceRequirement(), new WorkspacePermissionRequirement(policyName))
            .Build();
    }
}

public sealed class NetptuneAuthorizationOptions
{
    public string AuthenticationScheme { get; set; } = AuthenticationSchemes.Smart;
}
