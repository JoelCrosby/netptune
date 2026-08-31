using Netptune.App.Services;

namespace Netptune.App.Configuration;

public static class AppBroadcast
{
    // Declared on the route rather than in the handler so a write cannot ship without saying what it
    // changed, and so the broadcast only goes out once the handler has actually succeeded.
    public static RouteHandlerBuilder Broadcasts(this RouteHandlerBuilder builder, params string[] scopes)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);

            if (!Succeeded(result))
            {
                return result;
            }

            var boardEvents = context.HttpContext.RequestServices.GetRequiredService<IBoardEventService>();

            await boardEvents.BroadcastRequestAsync(context.HttpContext, scopes);

            return result;
        });
    }

    private static bool Succeeded(object? result)
    {
        var unwrapped = result is INestedHttpResult nested ? nested.Result : result;

        return unwrapped is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 };
    }
}
