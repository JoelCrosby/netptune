using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class CreateSprintAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateSprintAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_create_sprint";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Create };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A sprint name is required.");
        }

        var startDate = ReadDate(arguments, "startDate");
        var endDate = ReadDate(arguments, "endDate");

        if (startDate is null || endDate is null)
        {
            return AiToolExecution.Failed("A startDate and endDate as YYYY-MM-DD are required.");
        }

        var endsBeforeItStarts = endDate < startDate;

        if (endsBeforeItStarts)
        {
            return AiToolExecution.Failed("The sprint end date must not fall before its start date.");
        }

        var parent = await AiParentLookup.Project(Mediator, ChangeSet, arguments, cancellationToken);

        if (parent.Error is not null)
        {
            return AiToolExecution.Failed(parent.Error);
        }

        var project = parent.Parent!;

        var goal = AiToolSchema.GetString(arguments, "goal");
        var refKey = ChangeSet.CreateRefKey();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "project", After = project.Name },
            AiChangeFields.Date("startDate", null, startDate),
            AiChangeFields.Date("endDate", null, endDate),
        };

        AiToolSchema.AddOptionalField(fields, "goal", goal);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = "sprint",
            RefKey = refKey,
            Summary = Localized.Format(AppText.AiSummaryCreateSprint, name, project.Name),
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating sprint \"{name}\" as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static DateOnly? ReadDate(JsonElement arguments, string name)
    {
        var raw = AiToolSchema.GetString(arguments, name);
        var isParsed = DateOnly.TryParse(raw, out var parsed);

        return isParsed ? parsed : null;
    }
}
