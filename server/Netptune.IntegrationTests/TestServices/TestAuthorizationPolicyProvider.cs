using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Netptune.Identity.Authorization;

using ExternalAuthenticationSchemes = Netptune.Identity.Authentication.AuthenticationSchemes;

namespace Netptune.IntegrationTests.TestServices;

public class TestAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly NetptuneAuthorizationPolicyProvider Fallback;

    public TestAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        Fallback = new NetptuneAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => Fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName is ExternalAuthenticationSchemes.Github
            or ExternalAuthenticationSchemes.Google
            or ExternalAuthenticationSchemes.Microsoft)
        {
            return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(policyName)
                .Build());
        }

        return Fallback.GetPolicyAsync(policyName);
    }
}
