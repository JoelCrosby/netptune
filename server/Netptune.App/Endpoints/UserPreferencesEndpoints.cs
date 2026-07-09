using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Mediator;

using Netptune.Core.Preferences;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.UserPreferences.Commands;
using Netptune.Handlers.UserPreferences.Queries;

namespace Netptune.App.Endpoints;

public static class UserPreferencesEndpoints
{
    public static RouteGroupBuilder MapUserPreferencesEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("user-preferences")
            .RequireAuthorization();

        group.MapGet("/definitions", HandleGetDefinitions);
        group.MapGet("/values", HandleGetValues);
        group.MapPut("/values/{key}", HandlePutValue);
        group.MapDelete("/values/{key}", HandleDeleteValue);

        return builder;
    }

    private static async Task<IResult> HandleGetDefinitions(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPreferenceDefinitionsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetValues(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserPreferenceValuesQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePutValue(
        string key,
        HttpRequest httpRequest,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var request = await JsonSerializer.DeserializeAsync<UpdatePreferenceValueRequest>(
            httpRequest.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);

        if (request is null)
        {
            return Results.BadRequest(ClientResponse<ResolvedPreferenceValue>.Failed("Invalid preference request."));
        }

        var result = await mediator.Send(
            new SetUserPreferenceValueCommand
            {
                Key = key,
                Scope = request.Scope,
                Value = request.Value,
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteValue(
        string key,
        [FromQuery] string scope,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DeleteUserPreferenceValueCommand(key, scope),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
}
