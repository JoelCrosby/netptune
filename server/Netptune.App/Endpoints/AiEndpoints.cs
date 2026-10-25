using System.Globalization;
using System.Text.Json;

using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.App.Configuration;
using Netptune.App.Services;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Services.Ai;
using Netptune.Core.Services.Realtime;
using Netptune.Handlers.Ai.Commands;
using Netptune.Handlers.Ai.Queries;
using Netptune.Handlers.AiReviews.Commands;
using Netptune.Handlers.AiReviews.Queries;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.App.Endpoints;

public static class AiEndpoints
{
    private const string EventStreamContentType = "text/event-stream";

    private static readonly JsonSerializerOptions EventSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("ai")
            .WithTags("AI assistant")
            .RequireAuthorization();

        group.MapGet("/models", HandleGetModels)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/credentials", HandleGetCredentials)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapPut("/credentials", HandleSaveCredential)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapDelete("/credentials/{credentialId:guid}", HandleDeleteCredential)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapPatch("/credentials/{credentialId:guid}", HandleUpdateCredentialModels)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/credentials/availability", HandleGetCredentialAvailability)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/workspace-credentials", HandleGetWorkspaceCredentials)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapPut("/workspace-credentials", HandleSaveWorkspaceCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapDelete("/workspace-credentials/{credentialId:guid}", HandleDeleteWorkspaceCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapPatch("/workspace-credentials/{credentialId:guid}", HandleUpdateWorkspaceCredentialModels)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapGet("/workspace-search-credential", HandleGetWorkspaceSearchCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapPut("/workspace-search-credential", HandleSaveWorkspaceSearchCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapDelete("/workspace-search-credential", HandleDeleteWorkspaceSearchCredential)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapGet("/conversations", HandleGetConversations)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/conversations/{conversationId:guid}", HandleGetConversation)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapDelete("/conversations/{conversationId:guid}", HandleDeleteConversation)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group
            .MapPost("/conversations/messages", HandleSendMessage)
            .RequireAuthorization(NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group.MapPost("/conversations/{conversationId:guid}/stop", HandleStopTurn)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/admin/conversations", HandleGetWorkspaceConversations)
            .RequireAuthorization(NetptunePermissions.Assistant.ReadAllConversations);

        group.MapGet("/admin/conversations/{conversationId:guid}", HandleGetWorkspaceConversation)
            .RequireAuthorization(NetptunePermissions.Assistant.ReadAllConversations);

        group.MapDelete("/admin/conversations", HandleDeleteWorkspaceConversations)
            .RequireAuthorization(NetptunePermissions.Assistant.DeleteAnyConversations);

        group.MapGet("/admin/spend", HandleGetWorkspaceSpend)
            .RequireAuthorization(NetptunePermissions.Assistant.ReadAllConversations);

        group.MapPut("/admin/spend-cap", HandleSetSpendCap)
            .RequireAuthorization(NetptunePermissions.Workspace.Update);

        group.MapGet("/conversations/{conversationId:guid}/change-set", HandleGetPendingChangeSet)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/conversations/{conversationId:guid}/change-sets", HandleGetConversationChangeSets)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapGet("/change-sets/{changeSetId:guid}", HandleGetChangeSet)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapPost("/change-sets/{changeSetId:guid}/discard", HandleDiscardChangeSet)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group.MapPatch("/change-sets/{changeSetId:guid}/changes/{changeId:long}", HandleUpdateChange)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group
            .MapPost("/change-sets/{changeSetId:guid}/apply", HandleApplyChangeSet)
            .RequireAuthorization(NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group.MapPost("/change-sets/{changeSetId:guid}/stop", HandleStopChangeSetApply)
            .RequireAuthorization(NetptunePermissions.Assistant.Use);

        group
            .MapPost("/change-sets/{changeSetId:guid}/undo", HandleUndoChangeSet)
            .RequireAuthorization(NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/change-sets/{changeSetId:guid}/retry", HandleRetryChangeSet)
            .RequireAuthorization(NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/tasks/{systemId}/description", HandleWriteTaskDescription)
            .RequireAuthorization(
                NetptunePermissions.Tasks.Read,
                NetptunePermissions.Tasks.Update,
                NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/pages/{pageId:int}/write", HandleWritePage)
            .RequireAuthorization(
                NetptunePermissions.Pages.Read,
                NetptunePermissions.Pages.Update,
                NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/pages/transform", HandleTransformPageText)
            .RequireAuthorization(NetptunePermissions.Pages.Read, NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        group
            .MapPost("/reviews/{runId:int}/findings/{findingId:long}/decide", HandleDecideReviewFinding)
            .Broadcasts(WorkspaceEventScopes.AiReview, WorkspaceEventScopes.Task, WorkspaceEventScopes.Tag);

        MapTaskReviewEndpoints(builder);

        return group;
    }

    private static void MapTaskReviewEndpoints(RouteGroupBuilder builder)
    {
        var tasks = builder.MapGroup("tasks")
            .WithTags("AI assistant")
            .RequireAuthorization();

        tasks
            .MapPost("/{taskId:int}/reviews", HandleRunTaskReview)
            .RequireAuthorization(NetptunePermissions.Tasks.Read, NetptunePermissions.Assistant.Use)
            .RequireRateLimiting(RateLimiterConfiguration.AiPolicyName);

        tasks.MapGet("/{taskId:int}/reviews/latest", HandleGetLatestTaskReview)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        tasks.MapGet("/{taskId:int}/reviews", HandleGetTaskReviewHistory)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        tasks.MapGet("/{taskId:int}/reviews/{runId:int}", HandleGetTaskReviewRun)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);
    }

    // Not tied to RequestAborted, like a conversation turn: the review is paid for once it starts, so it
    // finishes and is stored even when the dialog closes, bounded by the reviewer's own timeout.
    private static async Task HandleRunTaskReview(
        int taskId,
        HttpContext context,
        IAiReviewer reviewer,
        IBoardEventService boardEventService)
    {
        StartEventStream(context);

        await context.Response.Body.FlushAsync(context.RequestAborted);

        var subject = new AiReviewSubject(EntityType.Task, taskId);
        var clientConnected = true;

        await foreach (var reviewEvent in reviewer.Review(subject, CancellationToken.None))
        {
            var hasStoredRun = reviewEvent.RunId.HasValue;

            if (hasStoredRun)
            {
                await boardEventService.BroadcastRequestAsync(context, WorkspaceEventScopes.AiReview);
            }

            if (!clientConnected)
            {
                continue;
            }

            clientConnected = await TryWriteEvent(context, reviewEvent);
        }
    }

    private static async Task<IResult> HandleDecideReviewFinding(
        int runId,
        long findingId,
        DecideAiReviewFindingRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DecideAiReviewFindingCommand(runId, findingId, request);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetLatestTaskReview(
        int taskId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLatestAiReviewQuery(taskId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetTaskReviewHistory(
        int taskId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiReviewHistoryQuery(taskId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetTaskReviewRun(
        int taskId,
        int runId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiReviewRunQuery(taskId, runId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleTransformPageText(
        TransformPageTextRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new TransformPageTextCommand(request), cancellationToken);

        return result.ToResult();
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

    private static async Task<IResult> HandleUpdateCredentialModels(
        Guid credentialId,
        UpdateAiCredentialModelsRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateAiCredentialModelsCommand(credentialId, request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleUpdateWorkspaceCredentialModels(
        Guid credentialId,
        UpdateAiCredentialModelsRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkspaceAiCredentialModelsCommand(credentialId, request);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDeleteCredential(
        Guid credentialId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAiCredentialCommand(credentialId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetModels(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiModelsQuery(), cancellationToken);

        return Results.Ok(result);
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

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetConversations(
        IMediator mediator,
        [AsParameters] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiConversationsQuery(request), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetConversation(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiConversationQuery(conversationId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDeleteConversation(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAiConversationCommand(conversationId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetWorkspaceConversations(
        IMediator mediator,
        [AsParameters] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceAiConversationsQuery(request), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteWorkspaceConversations(
        IMediator mediator,
        [FromBody] Guid[] conversationIds,
        CancellationToken cancellationToken)
    {
        var command = new DeleteWorkspaceAiConversationsCommand(conversationIds);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetWorkspaceSpend(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceAiSpendQuery(), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleSetSpendCap(
        SetAiSpendCapRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetAiSpendCapCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetWorkspaceConversation(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceAiConversationQuery(conversationId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetChangeSet(
        Guid changeSetId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAiChangeSetQuery(changeSetId), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGetPendingChangeSet(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPendingAiChangeSetQuery(conversationId), cancellationToken);

        return result.ToResult();
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

        return result.ToResult();
    }

    private static async Task<IResult> HandleDiscardChangeSet(
        Guid changeSetId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DiscardAiChangeSetCommand(changeSetId), cancellationToken);

        return result.ToResult();
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

        return result.ToResult();
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

        using var culture = CultureScope.Use(request.Locale ?? CultureInfo.CurrentUICulture.Name);

        var clientConnected = true;

        // Deliberately not tied to RequestAborted: the turn finishes and is stored after a reload, and
        // the service bounds it with Ai:TurnTimeoutSeconds.
        await foreach (var streamEvent in service.SendMessage(request, CancellationToken.None))
        {
            if (!clientConnected)
            {
                continue;
            }

            clientConnected = await TryWriteEvent(context, streamEvent);
        }
    }

    private static Task HandleWriteTaskDescription(
        string systemId,
        InlineWriteRequest request,
        HttpContext context,
        IAiInlineWriter writer)
    {
        var events = writer.WriteTaskDescription(systemId, request, context.RequestAborted);

        return StreamInlineWrite(context, events);
    }

    private static Task HandleWritePage(
        int pageId,
        InlineWriteRequest request,
        HttpContext context,
        IAiInlineWriter writer)
    {
        var events = writer.WritePage(pageId, request, context.RequestAborted);

        return StreamInlineWrite(context, events);
    }

    // Tied to RequestAborted, unlike a conversation turn: nothing is stored, so a writer who
    // cancels or leaves has no use for the rest of the reply.
    private static async Task StreamInlineWrite(HttpContext context, IAsyncEnumerable<AiStreamEvent> events)
    {
        StartEventStream(context);

        await foreach (var streamEvent in events)
        {
            var isWritten = await TryWriteEvent(context, streamEvent);

            if (!isWritten)
            {
                return;
            }
        }
    }

    private static async Task<IResult> HandleStopTurn(
        Guid conversationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StopAiTurnCommand(conversationId), cancellationToken);

        return result.ToResult();
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
