using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListPageVersionsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListPageVersionsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_page_versions";

    public string Description =>
        "List the saved versions of a live page, newest first: id, revision, title, savedAt, author, "
        + "and isCurrent for the version that matches the page now. "
        + "Pass a version id to propose_page_change restore_version to return the page to it.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "pageId": { "type": "integer", "description": "The page, from list_pages, search_pages, find_in_pages or get_page." }
        }
        """,
        "pageId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pageId = AiToolSchema.GetInt(arguments, "pageId");

        if (!pageId.HasValue)
        {
            return AiToolExecution.Failed("A pageId is required.");
        }

        var page = await AiPageLookup.Read(Mediator, pageId.Value, cancellationToken);

        if (page is null)
        {
            return AiToolExecution.Failed($"Page {pageId} is not a live page in this workspace.");
        }

        var result = await Mediator.Send(new GetPageVersionsQuery(page.Id), cancellationToken);
        var versions = result.Payload ?? [];
        var listed = versions.Select(version => new
        {
            id = version.Id,
            revision = version.Revision,
            title = version.Title,
            savedAt = version.SavedAt,
            author = version.CreatedByDisplayName,
            isCurrent = version.Revision == page.Revision,
        });

        var content = JsonSerializer.Serialize(listed);

        return AiToolExecution.Success(content);
    }
}
