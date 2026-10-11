using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class RestorePageAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public RestorePageAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_restore_page";

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
            return AiToolExecution.Failed(AiPageLookup.NotArchivedMessage(pageId.Value));
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page.Id,
            Summary = $"Restore “{page.Title}” from the archive",
            Fields = [AiChangeFields.Text("name", null, page.Title)],
            Payload = JsonSerializer.SerializeToDocument(new { pageId = page.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed restoring page {page.Id}, with any pages archived along with it. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
