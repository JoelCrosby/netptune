using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class UpdateCommentAction : IAiToolAction
{
    public const string Change = "propose_update_comment";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;
    private readonly IIdentityService Identity;

    public UpdateCommentAction(IMediator mediator, IAiChangeSetBuilder changeSet, IIdentityService identity)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
        Identity = identity;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Comments.Create };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var commentId = AiToolSchema.GetInt(arguments, "commentId");
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId || !commentId.HasValue)
        {
            return AiToolExecution.Failed("A systemId and a commentId are required.");
        }

        var body = AiToolSchema.GetString(arguments, "comment")?.Trim() ?? string.Empty;
        var isValidLength = body.Length is > 0 and <= AddCommentAction.MaximumCommentLength;

        if (!isValidLength)
        {
            return AiToolExecution.Failed($"A comment of 1 to {AddCommentAction.MaximumCommentLength} characters is required.");
        }

        var comment = await AiCommentLookup.Find(Mediator, systemId!, commentId.Value, cancellationToken);

        if (comment is null)
        {
            return AiToolExecution.Failed($"Comment {commentId} is not on task {systemId}.");
        }

        var isOwnComment = comment.UserId == Identity.GetCurrentUserId();

        if (!isOwnComment)
        {
            return AiToolExecution.Failed("Only the user's own comments can be edited.");
        }

        var isUnchanged = string.Equals(comment.Body, body, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return AiToolExecution.Failed("The comment already reads like that.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiCommentLookup.EntityType,
            EntityId = comment.Id,
            Summary = $"Edit comment on {systemId}",
            Fields = [AiChangeFields.Text("comment", comment.Body, body)],
            Payload = JsonSerializer.SerializeToDocument(new { systemId, commentId = comment.Id, comment = body }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed editing comment {comment.Id}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
