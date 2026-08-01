using Mediator;

using Netptune.Core.Requests.Ai;
using Netptune.Handlers.Ai.Commands;
using Netptune.Handlers.Ai.Queries;

namespace Netptune.App.Endpoints;

public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("ai")
            .WithTags("AI assistant")
            .RequireAuthorization();

        group.MapGet("/credentials", HandleGetCredentials);
        group.MapPut("/credentials", HandleSaveCredential);
        group.MapDelete("/credentials/{credentialId:guid}", HandleDeleteCredential);

        return group;
    }

    private static async Task<IResult> HandleGetCredentials(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiCredentialsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleSaveCredential(
        SaveAiCredentialRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SaveAiCredentialCommand(request), cancellationToken);

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleDeleteCredential(
        Guid credentialId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAiCredentialCommand(credentialId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }
}
