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
            ["update_details"] = new UpdatePageDetailsAction(mediator, changeSet),
            ["archive"] = new ArchivePageAction(mediator, changeSet),
            ["restore"] = new RestorePageAction(mediator, changeSet),
            ["delete"] = new DeletePageAction(mediator, changeSet),
            ["restore_version"] = new RestorePageVersionAction(mediator, changeSet),
        });
    }

    public string Name => "propose_page_change";

    public string Description =>
        "Propose a change to a page or folder. "
        + "create: a page or folder. "
        + "update_body: replaces the whole body with the markdown you give, so read the page with get_page first "
        + "and send every part you keep; pass the revision get_page returned. It is refused if the page changed since, "
        + "and applies only if nobody edits it before the user applies. "
        + "move: puts the page last under its new parent in the same space. "
        + "update_details: sets status (draft or published) and/or tags, the complete set of tag names to keep. "
        + "archive: archives the page and every page inside it; they can be restored. "
        + "restore: brings back a page from list_archived_pages. "
        + "restore_version: returns a page to a version from list_page_versions, saved as a new version. "
        + "delete: permanently deletes an archived page and everything inside it, and cannot be undone. "
        + "Only use delete when the user explicitly asks to delete permanently; otherwise archive.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": {
            "type": "string",
            "enum": ["create", "update_body", "move", "update_details", "archive", "restore", "delete", "restore_version"]
          },
          "pageId": { "type": "integer", "description": "Every action but create: the page, from list_pages, search_pages, find_in_pages, get_page or list_archived_pages." },
          "pageRef": { "type": "string", "description": "update_details: handle of a page proposed earlier in this change set, instead of pageId." },
          "title": { "type": "string", "description": "Create: the title. update_body: a new title; otherwise the current one stays." },
          "kind": { "type": "string", "enum": ["document", "folder"], "description": "Create: defaults to document." },
          "body": { "type": "string", "description": "Create: the markdown body of a document. update_body: the whole new body; link a task proposed earlier in this change set as [[task:<its handle>|name]], such as [[task:ref:3|Write the docs]]." },
          "revision": { "type": "integer", "description": "update_body: the revision get_page returned when you read the page." },
          "parentId": { "type": "integer", "description": "Create and move: the page or folder to put it under. Omit it, and parentRef, for the top level." },
          "parentRef": { "type": "string", "description": "Create and move: handle of a page or folder proposed earlier in this change set, instead of parentId." },
          "status": { "type": "string", "enum": ["draft", "published"], "description": "update_details: the new status." },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "update_details: the complete set of tag names, existing or proposed with propose_tag_change. An empty array clears them."
          },
          "versionId": { "type": "integer", "description": "restore_version: the version to return to, from list_page_versions." }
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
        return Actions.GetChangePermissions(changeName, payload);
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
