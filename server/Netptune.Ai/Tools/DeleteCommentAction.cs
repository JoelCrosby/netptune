using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class DeleteCommentAction : IAiToolAction
{
    public const string Change = "propose_delete_comment";

    private static readonly IReadOnlySet<string> DeleteOwnPermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Comments.DeleteOwn };

    private static readonly IReadOnlySet<string> DeleteAnyPermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Comments.DeleteAny };

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;
    private readonly IIdentityService Identity;

    public DeleteCommentAction(IMediator mediator, IAiChangeSetBuilder changeSet, IIdentityService identity)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
        Identity = identity;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions => DeleteOwnPermissions;

    public IReadOnlySet<string> GetChangePermissions(JsonElement payload)
    {
        var hasOwnership = payload.TryGetProperty("isOwn", out var isOwn);
        var isOwnComment = hasOwnership && isOwn.ValueKind == JsonValueKind.True;

        if (isOwnComment)
        {
            return DeleteOwnPermissions;
        }

        return DeleteAnyPermissions;
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var commentId = AiToolSchema.GetInt(arguments, "commentId");
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId || !commentId.HasValue)
        {
            return AiToolExecution.Failed("A systemId and a commentId are required.");
        }

        var comment = await AiCommentLookup.Find(Mediator, systemId!, commentId.Value, cancellationToken);

        if (comment is null)
        {
            return AiToolExecution.Failed($"Comment {commentId} is not on task {systemId}.");
        }

        var isOwnComment = comment.UserId == Identity.GetCurrentUserId();
        var fields = new List<AiChangeField>
        {
            AiChangeFields.Text("author", comment.UserDisplayName, null),
            AiChangeFields.Text("comment", comment.Body, null),
        };

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = AiCommentLookup.EntityType,
            EntityId = comment.Id,
            Summary = Localized.Format(AppText.AiSummaryDeleteComment, comment.UserDisplayName, systemId),
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { systemId, commentId = comment.Id, isOwn = isOwnComment }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        var ownershipNote = isOwnComment
            ? string.Empty
            : $"It is {comment.UserDisplayName}'s comment, so applying it needs permission to delete anyone's comments. ";

        return AiToolExecution.Success(
            $"Proposed deleting comment {comment.Id}. {ownershipNote}"
            + "Nothing has been deleted yet — the user must review and apply the change.");
    }
}
