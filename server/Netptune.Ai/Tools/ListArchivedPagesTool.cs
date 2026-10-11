using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class ListArchivedPagesTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListArchivedPagesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_archived_pages";

    public string Description =>
        "List the workspace's archived pages, most recently archived first: id, title, kind, archivedAt and archivedBy. "
        + "Pages archived along with a folder are inside it and come back with it, so only the folder is listed. "
        + "Use an id here to restore or permanently delete a page with propose_page_change.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var archived = await AiPageLookup.Archived(Mediator, cancellationToken);
        var pages = archived.Select(page => new
        {
            id = page.Id,
            title = page.Title,
            kind = page.Kind.ToString().ToLowerInvariant(),
            archivedAt = page.ArchivedAt,
            archivedBy = page.ArchivedByDisplayName,
        });

        var content = JsonSerializer.Serialize(pages);

        return AiToolExecution.Success(content);
    }
}
