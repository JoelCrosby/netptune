using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Services;
using Netptune.Core.Services.Realtime;

namespace Netptune.Api.Configuration;

public static class ApiBroadcast
{
    // Credentials have no realtime connection of their own, so every API write names a source no
    // browser can match. That stops connected clients from filtering the change out as one of their own.
    private const string SourceClientId = "api";

    // Writes made through the API have to reach open clients the same way writes made through the
    // app do, otherwise a board stays stale until somebody reloads it.
    public static RouteHandlerBuilder Broadcasts(this RouteHandlerBuilder builder, params string[] scopes)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);

            if (!Succeeded(result))
            {
                return result;
            }

            var identity = context.HttpContext.RequestServices.GetRequiredService<IIdentityService>();
            var workspaceKey = identity.TryGetWorkspaceKey();

            if (string.IsNullOrWhiteSpace(workspaceKey))
            {
                return result;
            }

            var publisher = context.HttpContext.RequestServices.GetRequiredService<IWorkspaceEventPublisher>();

            await publisher.PublishAsync(workspaceKey, SourceClientId, scopes);

            return result;
        });
    }

    private static bool Succeeded(object? result)
    {
        var unwrapped = result is INestedHttpResult nested ? nested.Result : result;

        return unwrapped is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 };
    }
}
