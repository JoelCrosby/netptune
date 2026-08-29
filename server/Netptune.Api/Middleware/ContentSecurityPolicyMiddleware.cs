using Scalar.AspNetCore;

namespace Netptune.Api.Middleware;

// The shared Traefik policy pins the client's inline script hash, which the generated docs page
// can never match. Scalar stamps a fresh nonce per request instead, and only this host knows it,
// so the API host carries its own policy rather than a static header from the gateway.
public sealed class ContentSecurityPolicyMiddleware(RequestDelegate next)
{
    private const string HeaderName = "Content-Security-Policy";

    private const string ApiPolicy = "default-src 'none'; frame-ancestors 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var nonce = context.Items[ScalarOptions.NonceHttpContextItemKey] as string;
            context.Response.Headers[HeaderName] = nonce is null ? ApiPolicy : DocsPolicy(nonce);

            return Task.CompletedTask;
        });

        await next(context);
    }

    private static string DocsPolicy(string nonce)
    {
        return "default-src 'self'; "
            + $"script-src 'self' 'nonce-{nonce}'; "
            + "style-src 'self' 'unsafe-inline'; "
            + "img-src 'self' data: https:; "
            + "font-src 'self' data:; "
            + "connect-src 'self'; "
            + "worker-src 'self' blob:; "
            + "frame-ancestors 'none'";
    }
}
