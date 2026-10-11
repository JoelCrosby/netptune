using System.Globalization;
using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class RestorePageVersionAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public RestorePageVersionAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_restore_page_version";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageId = AiToolSchema.GetInt(arguments, "pageId");
        var versionId = AiToolSchema.GetInt(arguments, "versionId");

        if (!pageId.HasValue || !versionId.HasValue)
        {
            return AiToolExecution.Failed("A pageId and a versionId are required.");
        }

        var page = await AiPageLookup.Read(Mediator, pageId.Value, cancellationToken);

        if (page is null)
        {
            return AiToolExecution.Failed($"Page {pageId} is not in this workspace.");
        }

        var versionResult = await Mediator.Send(new GetPageVersionQuery(page.Id, versionId.Value), cancellationToken);
        var version = versionResult.Payload;
        var isVersionFound = versionResult.IsSuccess && version is not null;

        if (!isVersionFound)
        {
            return AiToolExecution.Failed(
                $"Version {versionId} is not one of page {page.Id}'s versions. List them with list_page_versions.");
        }

        var isCurrentVersion = version!.Revision == page.Revision;

        if (isCurrentVersion)
        {
            return AiToolExecution.Failed($"Version {version.Id} is the page as it is now.");
        }

        var isFolder = page.Kind == PageKind.Folder;
        var currentBody = page.Body ?? string.Empty;
        var restoredBody = version.Body ?? string.Empty;
        var isRenamed = !string.Equals(version.Title, page.Title, StringComparison.Ordinal);
        var isRewritten = !isFolder && !string.Equals(restoredBody, currentBody, StringComparison.Ordinal);

        if (!isRenamed && !isRewritten)
        {
            return AiToolExecution.Failed($"Version {version.Id} has the same title and body as the page now.");
        }

        var fields = new List<AiChangeField>();

        if (isRenamed)
        {
            fields.Add(AiChangeFields.Text("name", page.Title, version.Title));
        }

        if (isRewritten)
        {
            fields.Add(AiChangeFields.Text("body", currentBody, restoredBody));
        }

        var payload = new Dictionary<string, object>
        {
            ["pageId"] = page.Id,
            ["versionId"] = version.Id,
            ["revision"] = page.Revision,
        };

        var savedOn = version.SavedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = AiPageLookup.EntityType,
            EntityId = page.Id,
            Summary = $"Restore “{page.Title}” to revision {version.Revision}, saved {savedOn}",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed restoring page {page.Id} to version {version.Id}; it is saved as a new version. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
