using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateCommentChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public const string ChangedSinceMessage =
        "The comment has been edited since this change was applied, so it was left as it is now.";

    private readonly IMediator Mediator;

    public UpdateCommentChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => UpdateCommentAction.Change;

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Comments.Create,
    };

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var body = AiChangePayload.ReadString(change.Payload.RootElement, "comment");
        var hasBody = !string.IsNullOrWhiteSpace(body);

        if (!hasBody)
        {
            return AiChangePayload.Failure(change, "The new comment text is missing from this change.");
        }

        var target = await FindTarget(change.Payload.RootElement, cancellationToken);

        if (target is null)
        {
            return AiChangePayload.Failure(change, "The comment this change refers to no longer exists.");
        }

        var error = await Save(target, body!, cancellationToken);

        if (error is not null)
        {
            return AiChangePayload.Failure(change, error);
        }

        return AiChangePayload.Applied(change, target.Id);
    }

    public async Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var target = await FindTarget(context.Change.Payload.RootElement, cancellationToken);

        if (target is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(new { comment = target.Body });
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = change.UndoPayload?.RootElement;

        if (!snapshot.HasValue)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var previousBody = AiChangePayload.ReadString(snapshot.Value, "comment");
        var hasPreviousBody = !string.IsNullOrWhiteSpace(previousBody);

        if (!hasPreviousBody)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var target = await FindTarget(change.Payload.RootElement, cancellationToken);

        if (target is null)
        {
            return AiChangeUndoResult.Failure(change, "The comment no longer exists.");
        }

        var appliedBody = AiChangePayload.ReadString(change.Payload.RootElement, "comment");
        var editedSince = !string.Equals(target.Body, appliedBody, StringComparison.Ordinal);

        if (editedSince)
        {
            return AiChangeUndoResult.Failure(change, ChangedSinceMessage);
        }

        var error = await Save(target, previousBody!, cancellationToken);

        if (error is not null)
        {
            return AiChangeUndoResult.Failure(change, error);
        }

        return AiChangeUndoResult.Undone(change, target.Id);
    }

    private async Task<CommentViewModel?> FindTarget(JsonElement payload, CancellationToken cancellationToken)
    {
        var systemId = AiChangePayload.ReadString(payload, "systemId");
        var commentId = AiChangePayload.ReadInt(payload, "commentId");

        if (systemId is null || !commentId.HasValue)
        {
            return null;
        }

        return await AiCommentLookup.Find(Mediator, systemId, commentId.Value, cancellationToken);
    }

    // The update command replaces the comment's mentions with the ones it is sent.
    private async Task<string?> Save(CommentViewModel comment, string body, CancellationToken cancellationToken)
    {
        var request = new UpdateCommentRequest
        {
            Comment = body,
            Mentions = comment.Mentions.Select(mention => mention.UserId).ToList(),
        };

        var response = await Mediator.Send(new UpdateCommentCommand(comment.Id, request), cancellationToken);

        if (response.IsSuccess)
        {
            return null;
        }

        return response.Message ?? "The comment could not be saved.";
    }
}
