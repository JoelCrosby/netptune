using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class MovePageAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public MovePageAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_move_page";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageId = AiToolSchema.GetInt(arguments, "pageId");

        if (!pageId.HasValue)
        {
            return AiToolExecution.Failed("A pageId is required.");
        }

        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var page = tree.FirstOrDefault(node => node.Id == pageId.Value);

        if (page is null)
        {
            return AiToolExecution.Failed($"Page {pageId} is not in this workspace.");
        }

        var parentId = AiToolSchema.GetInt(arguments, "parentId");
        var parentRef = AiPendingReference.Read(arguments, "parentRef");
        var lookup = AiPageLookup.ReadParent(tree, ChangeSet, parentId, parentRef);

        if (lookup.Parent is not { } parent)
        {
            return AiToolExecution.Failed(lookup.Error ?? "The parent could not be resolved.");
        }

        var movesIntoItself = parent.ParentId is { } targetId && AiPageLookup.IsInside(tree, targetId, page.Id);

        if (movesIntoItself)
        {
            return AiToolExecution.Failed("A page cannot be moved inside itself.");
        }

        var staysPut = parent.ParentRef is null && parent.ParentId == page.ParentId;

        if (staysPut)
        {
            return AiToolExecution.Failed($"“{page.Title}” is already there.");
        }

        var currentParent = tree.FirstOrDefault(node => node.Id == page.ParentId);
        var currentDisplay = currentParent?.Title ?? AiPageLookup.TopLevel;
        var payload = new Dictionary<string, object> { ["pageId"] = page.Id };

        if (parent.ParentId is { } resolvedParentId)
        {
            payload["parentId"] = resolvedParentId;
        }

        if (parent.ParentRef is { } resolvedParentRef)
        {
            payload["parentRef"] = resolvedParentRef;
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page.Id,
            Summary = $"Move “{page.Title}” to {parent.Display}",
            Fields = [AiChangeFields.Text("parent", currentDisplay, parent.Display)],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed moving page {page.Id} to {parent.Display}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
