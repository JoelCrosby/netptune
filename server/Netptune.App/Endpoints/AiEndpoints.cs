using System.Text.Json;

using Mediator;

using Netptune.App.Configuration;
using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Ai.Commands;
using Netptune.Handlers.Ai.Queries;

namespace Netptune.App.Endpoints;

public static class AiEndpoints
{
    private const string EventStreamContentType = "text/event-stream";

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

        group.MapGet("/workspace-search-credential", HandleGetWorkspaceSearchCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapPut("/workspace-search-credential", HandleSaveWorkspaceSearchCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapDelete("/workspace-search-credential", HandleDeleteWorkspaceSearchCredential)
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

        group.MapGet("/conversations/{conversationId:guid}/change-sets", HandleGetConversationChangeSets);

        group.MapGet("/change-sets/{changeSetId:guid}", HandleGetChangeSet);

        group.MapPost("/change-sets/{changeSetId:guid}/discard", HandleDiscardChangeSet);

        group.MapPatch("/change-sets/{changeSetId:guid}/changes/{changeId:long}", HandleUpdateChange);

        group
            .MapPost("/change-sets/{changeSetId:guid}/apply", HandleApplyChangeSet)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group.MapPost("/change-sets/{changeSetId:guid}/stop", HandleStopChangeSetApply);

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

    private static async Task<IResult> HandleGetWorkspaceSearchCredential(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceSearchCredentialQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleSaveWorkspaceSearchCredential(
        SaveWorkspaceSearchCredentialRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SaveWorkspaceSearchCredentialCommand(request), cancellationToken);

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleDeleteWorkspaceSearchCredential(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteWorkspaceSearchCredentialCommand(), cancellationToken);

        return result.IsSuccess ? Results.Ok(result) : Results.NotFound(result);
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

    private static async Task<IResult> HandleGetConversationChangeSets(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiConversationChangeSetsQuery(conversationId), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateChange(
        Guid changeSetId,
        long changeId,
        UpdateAiProposedChangeRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAiProposedChangeCommand(changeSetId, changeId, request);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleDiscardChangeSet(
        Guid changeSetId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DiscardAiChangeSetCommand(changeSetId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static Task<IResult> HandleApplyChangeSet(
        Guid changeSetId,
        ApplyAiChangeSetRequest request,
        IAiChangeSetApplier applier,
        IBoardEventService boardEventService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var wantsProgress = context.Request.Headers.Accept.Any(IsEventStream);

        if (wantsProgress)
        {
            return StreamChangeSetApply(changeSetId, request, applier, boardEventService, context, cancellationToken);
        }

        return RunChangeSetAction(
            () => applier.Apply(changeSetId, request, null, cancellationToken),
            boardEventService,
            context);
    }

    private static async Task<IResult> HandleStopChangeSetApply(
        Guid changeSetId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StopAiChangeSetApplyCommand(changeSetId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static bool IsEventStream(string? accept)
    {
        return accept?.Contains(EventStreamContentType, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    // The applier reports its first frame only once the change set has passed every check, so a
    // refusal still reaches the client as a status code rather than as a half written stream.
    private static async Task<IResult> StreamChangeSetApply(
        Guid changeSetId,
        ApplyAiChangeSetRequest request,
        IAiChangeSetApplier applier,
        IBoardEventService boardEventService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var progress = new ApplyProgressWriter(context);

        try
        {
            var result = await applier.Apply(changeSetId, request, progress.Write, cancellationToken);

            if (result is null)
            {
                return Results.NotFound();
            }

            await BroadcastApplied(result, boardEventService, context);
            await progress.Write(AiApplyProgress.Finished(result, result.Results.Count));

            return Results.Empty;
        }
        catch (UnauthorizedAccessException exception)
        {
            return await progress.Fail(exception.Message, StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException exception)
        {
            return await progress.Fail(exception.Message, StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            return Results.Empty;
        }
    }

    private static Task<IResult> HandleRetryChangeSet(
        Guid changeSetId,
        IAiChangeSetApplier applier,
        IBoardEventService boardEventService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        return RunChangeSetAction(
            () => applier.RetryFailed(changeSetId, cancellationToken),
            boardEventService,
            context);
    }

    private static Task<IResult> HandleUndoChangeSet(
        Guid changeSetId,
        IAiChangeSetApplier applier,
        IBoardEventService boardEventService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        return RunChangeSetAction(
            () => applier.Undo(changeSetId, cancellationToken),
            boardEventService,
            context);
    }

    private static async Task<IResult> RunChangeSetAction(Func<Task<AiApplyResult?>> action, IBoardEventService boardEventService, HttpContext context)
    {
        try
        {
            var result = await action();

            if (result is null)
            {
                return Results.NotFound();
            }

            await BroadcastApplied(result, boardEventService, context);

            return Results.Ok(result);
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

    private static async Task BroadcastApplied(
        AiApplyResult result,
        IBoardEventService boardEventService,
        HttpContext context)
    {
        var applied = result.Results.Where(change => change.Status == AiChangeApplyStatus.Applied).ToList();

        if (applied.Count == 0)
        {
            return;
        }

        var changedScopes = applied
            .Select(change => change.EntityType)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        await boardEventService.BroadcastRequestAsync(context, changedScopes);
    }

    private static void StartEventStream(HttpContext context)
    {
        context.Response.Headers.ContentType = EventStreamContentType;
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";
    }

    private static async Task HandleSendMessage(
        AiSendMessageRequest request,
        HttpContext context,
        IAiConversationService service)
    {
        StartEventStream(context);

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

    private static async Task<bool> TryWriteEvent<TEvent>(HttpContext context, TEvent streamEvent)
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

    private sealed class ApplyProgressWriter
    {
        private readonly HttpContext Context;

        private bool HasStarted;

        public ApplyProgressWriter(HttpContext context)
        {
            Context = context;
        }

        public async Task Write(AiApplyProgress progress)
        {
            if (!HasStarted)
            {
                HasStarted = true;

                StartEventStream(Context);
            }

            await TryWriteEvent(Context, progress);
        }

        public async Task<IResult> Fail(string message, int statusCode)
        {
            if (!HasStarted)
            {
                return Results.Problem(message, statusCode: statusCode);
            }

            await Write(AiApplyProgress.Failed(message));

            return Results.Empty;
        }
    }
}
