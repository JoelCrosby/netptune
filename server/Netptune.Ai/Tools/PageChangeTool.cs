using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class PageChangeTool : IAiTool
{
    private readonly AiToolActions Actions;

    public PageChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreatePageAction(mediator, changeSet),
            ["update_body"] = new UpdatePageBodyAction(mediator, changeSet),
            ["move"] = new MovePageAction(mediator, changeSet),
        });
    }

    public string Name => "propose_page_change";

    public string Description =>
        "Propose creating a page or folder, rewriting a page, or moving one. "
        + "update_body replaces the whole body with the markdown you give, so read the page with get_page first "
        + "and send every part you keep; it is saved as a new version the user can restore. "
        + "Pass the revision get_page returned; it is refused if the page changed since, and applies only if "
        + "nobody edits it before the user applies. Move puts the page last under its new parent.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update_body", "move"] },
          "pageId": { "type": "integer", "description": "update_body and move: the page, from search_pages or get_page." },
          "title": { "type": "string", "description": "Create: the title. update_body: a new title; otherwise the current one stays." },
          "kind": { "type": "string", "enum": ["document", "folder"], "description": "Create: defaults to document." },
          "body": { "type": "string", "description": "Create: the markdown body of a document. update_body: the whole new body; link a task proposed earlier in this change set as [[task:<its handle>|name]], such as [[task:ref:3|Write the docs]]." },
          "revision": { "type": "integer", "description": "update_body: the revision get_page returned when you read the page." },
          "parentId": { "type": "integer", "description": "Create and move: the page or folder to put it under. Omit it, and parentRef, for the top level." },
          "parentRef": { "type": "string", "description": "Create and move: handle of a page or folder proposed earlier in this change set, instead of parentId." }
        }
        """,
        "action");

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return Actions.IsAvailable(permissions);
    }

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        return Actions.GetRequiredPermissions(arguments);
    }

    public IReadOnlySet<string> GetChangePermissions(string changeName, JsonElement payload)
    {
        return Actions.GetChangePermissions(changeName);
    }

    public string DescribeCall(JsonElement arguments)
    {
        return Actions.DescribeCall(Name, arguments);
    }

    public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        return Actions.Execute(arguments, cancellationToken);
    }
}
