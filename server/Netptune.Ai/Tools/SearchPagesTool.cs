using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Search.Queries;

namespace Netptune.Ai.Tools;

public sealed class SearchPagesTool : IAiTool
{
    private const int DefaultLimit = 10;
    private const int MaximumLimit = 25;

    private static readonly string[] PageTypes = ["pages"];

    private readonly IMediator Mediator;

    public SearchPagesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "search_pages";

    public string Description =>
        "Full-text search across the workspace's pages: titles, folder paths, tags and body text. "
        + "Returns each match's id, slug, name, folder path and a short snippet of the body around the match. "
        + "Read a page in full with get_page.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "query": { "type": "string", "description": "Words to search for." },
          "limit": { "type": "integer", "description": "How many pages to return, up to 25. Defaults to 10." }
        }
        """,
        "query");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var query = AiToolSchema.GetString(arguments, "query")?.Trim();
        var hasQuery = !string.IsNullOrWhiteSpace(query);

        if (!hasQuery)
        {
            return AiToolExecution.Failed("A query is required.");
        }

        var requestedLimit = AiToolSchema.GetInt(arguments, "limit") ?? DefaultLimit;
        var limit = Math.Clamp(requestedLimit, 1, MaximumLimit);

        // The tool's own pages.read requirement is what lets it ask for pages.
        var search = new SearchQuery(query!, PageTypes, limit, IncludePages: true);
        var response = await Mediator.Send(search, cancellationToken);

        var pages = response.Results
            .Where(result => result.Type == "page")
            .Select(result => new
            {
                id = result.Id,
                slug = result.Metadata.GetValueOrDefault("slug"),
                name = result.Title,
                kind = result.Metadata.GetValueOrDefault("kind"),
                path = result.Metadata.GetValueOrDefault("path"),
                snippet = result.Metadata.GetValueOrDefault("snippet"),
            });

        var content = JsonSerializer.Serialize(pages);

        return AiToolExecution.Success(content);
    }
}
