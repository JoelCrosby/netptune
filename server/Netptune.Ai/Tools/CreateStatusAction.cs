using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateStatusAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateStatusAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_create_status";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Statuses.Manage };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A status name is required.");
        }

        var rawCategory = AiToolSchema.GetString(arguments, "category");
        var isKnownCategory = AiToolSchema.TryParseEnum<StatusCategory>(rawCategory, out var category);

        if (!isKnownCategory)
        {
            return AiToolExecution.Failed(
                "A category of New, Backlog, Todo, Active, Done or Inactive is required.");
        }

        var key = name!.ToUrlSlug();
        var hasKey = !string.IsNullOrWhiteSpace(key);

        if (!hasKey)
        {
            return AiToolExecution.Failed("A status name needs at least one letter or digit.");
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var existing = statuses?.FirstOrDefault(status => status.Key == key);

        if (existing is not null)
        {
            return AiToolExecution.Failed($"A status named \"{existing.Name}\" already exists.");
        }

        var refKey = ChangeSet.CreateRefKey();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "category", After = LocalizedEnum.Label(category) },
        };

        AiToolSchema.AddOptionalField(fields, "description", AiToolSchema.GetString(arguments, "description"));
        AiToolSchema.AddOptionalField(fields, "color", AiToolSchema.GetString(arguments, "color"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = "status",
            RefKey = refKey,
            Summary = Localized.Format(AppText.AiSummaryCreateStatus, name, LocalizedEnum.Label(category)),
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating status \"{name}\" as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
