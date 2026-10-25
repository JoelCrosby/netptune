using Netptune.App.Services;
using Netptune.Core.Responses.Common;

namespace Netptune.App.Configuration;

public static class AppBroadcast
{
    // Declared on the route rather than in the handler so a write cannot ship without saying what it
    // changed, and so the broadcast only goes out once the handler has actually succeeded.
    public static RouteHandlerBuilder Broadcasts(this RouteHandlerBuilder builder, params string[] scopes)
    {
        return builder.AddBroadcastFilter(_ => true, scopes);
    }

    public static RouteHandlerBuilder BroadcastsWhen<TPayload>(
        this RouteHandlerBuilder builder,
        Func<TPayload, bool> changed,
        params string[] scopes)
    {
        return builder.AddBroadcastFilter(result => PayloadChanged(result, changed), scopes);
    }

    private static RouteHandlerBuilder AddBroadcastFilter(
        this RouteHandlerBuilder builder,
        Func<object?, bool> changed,
        string[] scopes)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            var shouldBroadcast = Succeeded(result) && changed(result);

            if (!shouldBroadcast)
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
        var unwrapped = Unwrap(result);

        return unwrapped is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 };
    }

    private static bool PayloadChanged<TPayload>(object? result, Func<TPayload, bool> changed)
    {
        var unwrapped = Unwrap(result);

        if (unwrapped is not IValueHttpResult { Value: ClientResponse<TPayload> { Payload: { } payload } })
        {
            return false;
        }

        var payloadChanged = changed(payload);

        return payloadChanged;
    }

    private static object? Unwrap(object? result)
    {
        return result is INestedHttpResult nested ? nested.Result : result;
    }
}
