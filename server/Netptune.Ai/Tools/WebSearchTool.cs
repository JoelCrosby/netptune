using System.Text.Json;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class WebSearchTool : IAiTool
{
    private const int DefaultTake = 5;

    private readonly IWebSearchProvider Provider;

    public WebSearchTool(IWebSearchProvider provider)
    {
        Provider = provider;
    }

    public string Name => "web_search";

    public string Description =>
        "Search the public web and get back titles, URLs and snippets. "
        + "Follow a result with web_fetch to read the page itself.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Assistant.UseWeb };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "query": { "type": "string", "description": "What to search for." },
          "take": { "type": "integer", "description": "How many results to return. Defaults to 5." }
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

        var take = AiToolSchema.GetInt(arguments, "take") ?? DefaultTake;
        var result = await Provider.Search(query!, take, cancellationToken);

        if (!result.IsSuccess)
        {
            return AiToolExecution.Failed(result.Error ?? "The search failed.");
        }

        var summary = new
        {
            resultCount = result.Hits.Count,
            results = result.Hits.Select(hit => new
            {
                title = hit.Title,
                url = hit.Url,
                snippet = hit.Snippet,
            }),
        };

        var content = JsonSerializer.Serialize(summary);

        return AiToolExecution.Success(content);
    }
}
