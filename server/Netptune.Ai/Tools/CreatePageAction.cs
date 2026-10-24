using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Pages;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class CreatePageAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreatePageAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_create_page";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Create };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var title = AiToolSchema.GetString(arguments, "title")?.Trim();
        var hasTitle = !string.IsNullOrWhiteSpace(title);

        if (!hasTitle)
        {
            return AiToolExecution.Failed("A title is required.");
        }

        var normalisedTitle = PageTitle.Normalise(title);
        var titleTooLong = PageTitle.IsTooLong(normalisedTitle);

        if (titleTooLong)
        {
            return AiToolExecution.Failed(PageTitle.TooLongMessage);
        }

        var kind = AiPageLookup.ParseKind(AiToolSchema.GetString(arguments, "kind"));

        if (!kind.HasValue)
        {
            return AiToolExecution.Failed("kind must be document or folder.");
        }

        var body = AiToolSchema.GetString(arguments, "body");
        var isFolder = kind == PageKind.Folder;
        var hasBody = !string.IsNullOrEmpty(body);

        if (isFolder && hasBody)
        {
            return AiToolExecution.Failed("Folders have no body; propose the pages inside it instead.");
        }

        var linksPendingTasks = hasBody && AiPageLookup.ReadTaskHandles(body!).Count > 0;

        if (linksPendingTasks)
        {
            return AiToolExecution.Failed(
                "A new page cannot link to tasks proposed in the same change set. Create it without those links; once it is applied, add them with update_body.");
        }

        var bodyTooLarge = PageBody.IsTooLarge(body);

        if (bodyTooLarge)
        {
            return AiToolExecution.Failed(PageBody.TooLargeMessage);
        }

        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var parentId = AiToolSchema.GetInt(arguments, "parentId");
        var parentRef = AiPendingReference.Read(arguments, "parentRef");
        var lookup = AiPageLookup.ReadParent(tree, ChangeSet, parentId, parentRef);

        if (lookup.Parent is not { } parent)
        {
            return AiToolExecution.Failed(lookup.Error ?? "The parent could not be resolved.");
        }

        var kindName = kind.Value.ToString().ToLowerInvariant();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = normalisedTitle },
            new() { Name = "kind", After = LocalizedEnum.Label(kind.Value) },
            new() { Name = "parent", After = parent.Display },
        };

        if (hasBody)
        {
            fields.Add(new AiChangeField { Name = "body", After = body });
        }

        var payload = new Dictionary<string, object> { ["title"] = normalisedTitle, ["kind"] = kindName };

        if (hasBody)
        {
            payload["body"] = body!;
        }

        if (parent.ParentId is { } resolvedParentId)
        {
            payload["parentId"] = resolvedParentId;
        }

        if (parent.ParentRef is { } resolvedParentRef)
        {
            payload["parentRef"] = resolvedParentRef;
        }

        var refKey = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            RefKey = refKey,
            Summary = Localized.Format(DescribeCreate(kind.Value), normalisedTitle, parent.Display),
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating {kindName} \"{normalisedTitle}\" as {refKey}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static string DescribeCreate(PageKind kind)
    {
        return kind == PageKind.Folder ? AppText.AiSummaryCreateFolder : AppText.AiSummaryCreateDocument;
    }
}
