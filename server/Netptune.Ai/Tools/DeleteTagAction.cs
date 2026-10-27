using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public sealed class DeleteTagAction : IAiToolAction
{
    public const string Change = "propose_delete_tag";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteTagAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Delete };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var tagId = AiToolSchema.GetInt(arguments, "tagId");

        if (!tagId.HasValue)
        {
            return AiToolExecution.Failed("A tagId is required.");
        }

        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken) ?? [];
        var tag = tags.FirstOrDefault(item => item.Id == tagId.Value);

        if (tag is null)
        {
            return AiToolExecution.Failed($"Tag {tagId} is not in this workspace.");
        }

        var fields = new List<AiChangeField> { AiChangeFields.Text("tag", tag.Name, null) };
        var isInUse = tag.TaskCount > 0;

        if (isInUse)
        {
            fields.Add(AiChangeFields.Text("tasks", $"{tag.TaskCount}", null));
        }

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = "tag",
            EntityId = tag.Id,
            Summary = Localized.Format(AppText.AiSummaryDeleteTag, tag.Name),
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { tagId = tag.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting tag {tag.Id}. It also comes off every page that uses it. "
            + "Nothing has been deleted yet — the user must review and apply the change.");
    }
}
