using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class DeletePageAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeletePageAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_delete_page";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Delete };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageId = AiToolSchema.GetInt(arguments, "pageId");

        if (!pageId.HasValue)
        {
            return AiToolExecution.Failed("A pageId is required.");
        }

        var archived = await AiPageLookup.Archived(Mediator, cancellationToken);
        var page = archived.FirstOrDefault(candidate => candidate.Id == pageId.Value);

        if (page is null)
        {
            return AiToolExecution.Failed(
                $"{AiPageLookup.NotArchivedMessage(pageId.Value)} Only archived pages can be deleted permanently; archive a live page instead.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page.Id,
            Summary = Localized.Format(AppText.AiSummaryDeletePage, page.Title),
            Fields = [AiChangeFields.Text("name", page.Title, null)],
            Payload = JsonSerializer.SerializeToDocument(new { pageId = page.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed permanently deleting page {page.Id}. This cannot be undone once applied. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
