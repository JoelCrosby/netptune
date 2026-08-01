using System.Text.Json;

using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Ai.Commands;
using Netptune.Handlers.Ai.Queries;

namespace Netptune.App.Endpoints;

public static class AiEndpoints
{
    private static readonly TimeSpan TurnTimeout = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions EventSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("ai")
            .WithTags("AI assistant")
            .RequireAuthorization();

        group.MapGet("/credentials", HandleGetCredentials);
        group.MapPut("/credentials", HandleSaveCredential);
        group.MapDelete("/credentials/{credentialId:guid}", HandleDeleteCredential);

        group.MapGet("/conversations", HandleGetConversations);
        group.MapGet("/conversations/{conversationId:guid}", HandleGetConversation);
        group.MapDelete("/conversations/{conversationId:guid}", HandleDeleteConversation);
        group.MapPost("/conversations/messages", HandleSendMessage);

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

    private static async Task<IResult> HandleGetConversations(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiConversationsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetConversation(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiConversationQuery(conversationId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteConversation(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAiConversationCommand(conversationId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task HandleSendMessage(
        AiSendMessageRequest request,
        HttpContext context,
        IAiConversationService service)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        using var turnCancellation = new CancellationTokenSource(TurnTimeout);
        var clientConnected = true;

        await foreach (var streamEvent in service.SendMessage(request, turnCancellation.Token))
        {
            if (!clientConnected)
            {
                continue;
            }

            clientConnected = await TryWriteEvent(context, streamEvent);
        }
    }

    private static async Task<bool> TryWriteEvent(HttpContext context, AiStreamEvent streamEvent)
    {
        try
        {
            var payload = JsonSerializer.Serialize(streamEvent, EventSerializerOptions);

            await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
