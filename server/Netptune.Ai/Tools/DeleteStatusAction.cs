using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class DeleteStatusAction : IAiToolAction
{
    public const string Change = "propose_delete_status";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteStatusAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Statuses.Manage };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var statusId = AiToolSchema.GetInt(arguments, "statusId");

        if (!statusId.HasValue)
        {
            return AiToolExecution.Failed("A statusId is required.");
        }

        var usage = await Mediator.Send(new GetStatusUsageQuery(statusId.Value), cancellationToken);

        if (usage is null)
        {
            return AiToolExecution.Failed($"Status {statusId} is not in this workspace.");
        }

        if (!usage.CanDelete)
        {
            return AiToolExecution.Failed(usage.BlockedReason ?? $"Status “{usage.Name}” cannot be deleted.");
        }

        var fields = new List<AiChangeField> { AiChangeFields.Text("status", usage.Name, null) };

        AiUsageFields.Add(fields, usage);

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = "status",
            EntityId = usage.Id,
            Summary = Localized.Format(AppText.AiSummaryDeleteStatus, usage.Name),
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { statusId = usage.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting status {usage.Id}. "
            + "Nothing has been deleted yet — the user must review and apply the change."
            + AiUsageFields.Describe(usage));
    }
}
