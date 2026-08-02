using System.Text.Json;

using Mediator;

using Netptune.App.Configuration;
using Netptune.Core.Authorization;
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

        group.MapGet("/models", () => Results.Ok(AiModels.Catalog));

        group.MapGet("/credentials", HandleGetCredentials);
        group.MapPut("/credentials", HandleSaveCredential);
        group.MapDelete("/credentials/{credentialId:guid}", HandleDeleteCredential);
        group.MapGet("/credentials/availability", HandleGetCredentialAvailability);

        group.MapGet("/workspace-credentials", HandleGetWorkspaceCredentials)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);
        group.MapPut("/workspace-credentials", HandleSaveWorkspaceCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);
        group.MapDelete("/workspace-credentials/{credentialId:guid}", HandleDeleteWorkspaceCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapGet("/conversations", HandleGetConversations);
        group.MapGet("/conversations/{conversationId:guid}", HandleGetConversation);
        group.MapDelete("/conversations/{conversationId:guid}", HandleDeleteConversation);

        group
            .MapPost("/conversations/messages", HandleSendMessage)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group.MapPost("/conversations/{conversationId:guid}/stop", HandleStopTurn);

        group.MapGet("/admin/conversations", HandleGetWorkspaceConversations)
            .RequireAuthorization(NetptunePermissions.Assistant.ReadAllConversations);
        group.MapGet("/admin/conversations/{conversationId:guid}", HandleGetWorkspaceConversation)
            .RequireAuthorization(NetptunePermissions.Assistant.ReadAllConversations);

        group.MapGet("/conversations/{conversationId:guid}/change-set", HandleGetPendingChangeSet);

        group.MapGet("/change-sets/{changeSetId:guid}", HandleGetChangeSet);
        group.MapPost("/change-sets/{changeSetId:guid}/discard", HandleDiscardChangeSet);

        group
            .MapPost("/change-sets/{changeSetId:guid}/apply", HandleApplyChangeSet)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/change-sets/{changeSetId:guid}/undo", HandleUndoChangeSet)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/change-sets/{changeSetId:guid}/retry", HandleRetryChangeSet)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

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

    private static async Task<IResult> HandleGetCredentialAvailability(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiCredentialAvailabilityQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetWorkspaceCredentials(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceAiCredentialsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleSaveWorkspaceCredential(
        SaveAiCredentialRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SaveWorkspaceAiCredentialCommand(request), cancellationToken);

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleDeleteWorkspaceCredential(
        Guid credentialId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteWorkspaceAiCredentialCommand(credentialId), cancellationToken);

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

    private static async Task<IResult> HandleGetWorkspaceConversations(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceAiConversationsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetWorkspaceConversation(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceAiConversationQuery(conversationId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleGetChangeSet(
        Guid changeSetId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiChangeSetQuery(changeSetId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleGetPendingChangeSet(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPendingAiChangeSetQuery(conversationId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleDiscardChangeSet(
        Guid changeSetId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DiscardAiChangeSetCommand(changeSetId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleApplyChangeSet(
        Guid changeSetId,
        ApplyAiChangeSetRequest request,
        IAiChangeSetApplier applier,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await applier.Apply(changeSetId, request, cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> HandleRetryChangeSet(
        Guid changeSetId,
        IAiChangeSetApplier applier,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await applier.RetryFailed(changeSetId, cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> HandleUndoChangeSet(
        Guid changeSetId,
        IAiChangeSetApplier applier,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await applier.Undo(changeSetId, cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
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

    private static async Task<IResult> HandleStopTurn(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StopAiTurnCommand(conversationId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
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
