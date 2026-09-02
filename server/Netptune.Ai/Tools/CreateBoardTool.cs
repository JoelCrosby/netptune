using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Boards.Queries;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateBoardTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateBoardTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_board";

    public string Description =>
        "Propose creating a board in a project. The board is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Boards.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The board name." },
          "projectId": { "type": "integer", "description": "The project the board belongs to, from list_projects." },
          "projectRef": { "type": "string", "description": "Handle of a project proposed earlier in this change set, instead of projectId." },
          "identifier": { "type": "string", "description": "Url identifier for the board, lowercase with dashes." }
        }
        """,
        "name",
        "identifier");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var identifier = AiToolSchema.GetString(arguments, "identifier")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasIdentifier = !string.IsNullOrWhiteSpace(identifier);

        if (!hasName || !hasIdentifier)
        {
            return AiToolExecution.Failed("A board name and identifier are required.");
        }

        var parent = await AiParentLookup.Project(Mediator, ChangeSet, arguments, cancellationToken);

        if (parent.Error is not null)
        {
            return AiToolExecution.Failed(parent.Error);
        }

        var project = parent.Parent!;

        var uniqueness = await Mediator.Send(new IsBoardIdentifierUniqueQuery(identifier!), cancellationToken);
        var isTaken = uniqueness.Payload?.IsUnique == false;

        if (isTaken)
        {
            return AiToolExecution.Failed($"The board identifier \"{identifier}\" is already in use.");
        }

        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "identifier", After = identifier },
            new() { Name = "project", After = project.Name },
        };

        var refKey = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "board",
            RefKey = refKey,
            Summary = $"Create board “{name}” in {project.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating board \"{name}\" as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
