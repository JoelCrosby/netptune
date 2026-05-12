using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

using Netptune.Identity.Authentication;
using Netptune.Identity.Authorization.Requirements;

namespace Netptune.IntegrationTests.TestServices;

public class TestPolicyEvaluator(PolicyEvaluator innerEvaluator) : IPolicyEvaluator
{
    public Task<AuthenticateResult> AuthenticateAsync(
        AuthorizationPolicy policy,
        HttpContext context)
    {
        var providerScheme = GetExternalProviderScheme(policy);

        if (providerScheme is not null)
        {
            return Task.FromResult(AuthenticateExternalProvider(providerScheme, context));
        }

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
        if (GetExternalProviderScheme(policy) is not null)
        {
            return Task.FromResult(authenticationResult.Succeeded
                ? PolicyAuthorizationResult.Success()
                : PolicyAuthorizationResult.Forbid());
        }

        return innerEvaluator.AuthorizeAsync(
            policy,
            authenticationResult,
            context,
            resource);
    }

    private static string? GetExternalProviderScheme(AuthorizationPolicy policy)
    {
        if (policy.AuthenticationSchemes.Contains(AuthenticationSchemes.Github))
        {
            return AuthenticationSchemes.Github;
        }

        if (policy.AuthenticationSchemes.Contains(AuthenticationSchemes.Google))
        {
            return AuthenticationSchemes.Google;
        }

        if (policy.AuthenticationSchemes.Contains(AuthenticationSchemes.Microsoft))
        {
            return AuthenticationSchemes.Microsoft;
        }

        var permissionRequirement = policy.Requirements
            .OfType<WorkspacePermissionRequirement>()
            .FirstOrDefault(requirement =>
                requirement.Permission is AuthenticationSchemes.Github
                    or AuthenticationSchemes.Google
                    or AuthenticationSchemes.Microsoft);

        if (permissionRequirement is not null)
        {
            return permissionRequirement.Permission;
        }

        return null;
    }

    private static AuthenticateResult AuthenticateExternalProvider(string providerScheme, HttpContext context)
    {
        var email = GetHeader(context, "x-test-auth-email", $"{providerScheme.ToLowerInvariant()}-user@example.com");
        var providerKey = GetHeader(context, "x-test-auth-provider-key", $"{providerScheme.ToLowerInvariant()}-provider-key");
        var name = GetHeader(context, "x-test-auth-name", $"{providerScheme} User");
        var pictureUrl = GetHeader(context, "x-test-auth-picture", "https://example.com/avatar.png");
        var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, providerKey),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new("urn:github:name", name),
            new(ClaimTypes.GivenName, nameParts.ElementAtOrDefault(0) ?? name),
            new(ClaimTypes.Surname, nameParts.ElementAtOrDefault(1) ?? string.Empty),
            new("Provider-Picture-Url", pictureUrl),
        };

        var identity = new ClaimsIdentity(claims, providerScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, providerScheme);

        context.User = principal;

        return AuthenticateResult.Success(ticket);
    }

    private static string GetHeader(HttpContext context, string name, string fallback)
    {
        return context.Request.Headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : fallback;
    }
}
