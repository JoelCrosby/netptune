using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Comments.Commands;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class AddTaskCommentChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public AddTaskCommentChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_add_comment";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = AiChangePayload.ResolveTaskId(context);
        var comment = AiChangePayload.ReadString(change.Payload.RootElement, "comment");
        var hasComment = !string.IsNullOrWhiteSpace(comment);

        if (!taskId.HasValue || !hasComment)
        {
            return AiChangePayload.Failure(change, "The task or comment this change refers to could not be resolved.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiChangePayload.Failure(change, $"Task {taskId} was not found.");
        }

        var request = new AddCommentRequest
        {
            Comment = comment!,
            SystemId = task.SystemId,
        };

        var response = await Mediator.Send(new AddCommentToTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The comment could not be posted.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Comments.DeleteOwn,
    };

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public async Task<AiAppliedChangeResult> Revert(
        AiChangeUndoContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var commentId = change.AppliedEntityId;

        if (!commentId.HasValue)
        {
            return AiChangeUndoResult.Failure(change, "The posted comment could not be resolved.");
        }

        var response = await Mediator.Send(new DeleteCommentCommand(commentId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? "The comment could not be removed.");
        }

        return AiChangeUndoResult.Undone(change, commentId);
    }
}
