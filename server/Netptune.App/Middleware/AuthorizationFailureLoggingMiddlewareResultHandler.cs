using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Netptune.App.Middleware;

public sealed class AuthorizationFailureLoggingMiddlewareResultHandler(
    ILogger<AuthorizationFailureLoggingMiddlewareResultHandler> logger) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler DefaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded && IsAuthProviderRoute(context))
        {
            logger.LogWarning(
                "External auth authorization did not succeed. Path: {Path}; Status: {Status}; PolicySchemes: {PolicySchemes}; Endpoint: {Endpoint}; QueryKeys: {QueryKeys}; CookieNames: {CookieNames}; UserIdentities: {UserIdentities}; FailureReasons: {FailureReasons}",
                context.Request.Path.Value,
                GetAuthorizationStatus(authorizeResult),
                Join(policy.AuthenticationSchemes),
                context.GetEndpoint()?.DisplayName,
                Join(context.Request.Query.Keys),
                Join(context.Request.Cookies.Keys),
                Join(context.User.Identities.Select(identity => $"{identity.AuthenticationType}:{identity.IsAuthenticated}")),
                authorizeResult.AuthorizationFailure is null
                    ? null
                    : Join(authorizeResult.AuthorizationFailure.FailureReasons.Select(reason => reason.Message)));
        }

        return DefaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static bool IsAuthProviderRoute(HttpContext context)
    {
        var path = context.Request.Path.Value;

        return path?.Contains("-login-redirect", StringComparison.OrdinalIgnoreCase) is true
            || path?.Contains("-login-complete", StringComparison.OrdinalIgnoreCase) is true;
    }

    private static string GetAuthorizationStatus(PolicyAuthorizationResult result)
    {
        if (result.Succeeded) return "Succeeded";
        if (result.Forbidden) return "Forbidden";
        if (result.Challenged) return "Challenged";

        return "Failed";
    }

    private static string Join(IEnumerable<string?> values)
    {
        var joined = string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(joined) ? "<none>" : joined;
    }
}
