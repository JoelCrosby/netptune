using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class TurnstileEndpoints
{
    public static RouteGroupBuilder MapTurnstileEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("turnstile");

        group.MapPost("/validate", HandleValidate).AllowAnonymous();

        return group;
    }

    public static async Task<IResult> HandleValidate(
        ITurnstileService turnstileService,
        ValidateTurnstileRequest request,
        HttpContext context)
    {
        var remoteIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
            ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString();

        var success = await turnstileService.ValidateAsync(request.Token, remoteIp);

        if (!success)
        {
            return Results.BadRequest("Turnstile validation failed.");
        }

        return Results.Ok();
    }
}
