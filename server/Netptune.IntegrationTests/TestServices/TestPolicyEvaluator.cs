using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Netptune.IntegrationTests.TestServices;

public class TestPolicyEvaluator(PolicyEvaluator innerEvaluator) : IPolicyEvaluator
{
    public Task<AuthenticateResult> AuthenticateAsync(
        AuthorizationPolicy policy,
        HttpContext context)
    {
        var combinedPolicy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(TestAuthenticationHandler.AuthenticationScheme)
            .Combine(policy)
            .Build();

        return innerEvaluator.AuthenticateAsync(combinedPolicy, context);
    }

    public Task<PolicyAuthorizationResult> AuthorizeAsync(
        AuthorizationPolicy policy,
        AuthenticateResult authenticationResult,
        HttpContext context,
        object? resource)
    {
        return innerEvaluator.AuthorizeAsync(
            policy,
            authenticationResult,
            context,
            resource);
    }
}
