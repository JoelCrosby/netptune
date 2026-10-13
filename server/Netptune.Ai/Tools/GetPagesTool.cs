using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetPagesTool : IAiTool
{
    private const int MaximumPages = 10;
    private const int DefaultTake = 4000;
    private const int TotalBudget = 24_000;

    private readonly IMediator Mediator;

    public GetPagesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_pages";

    public string Description =>
        "Read up to 10 pages at once by slug, each in the same shape as get_page. "
        + "Every page's body starts at offset 0; a 24000-character budget is shared between them, "
        + "so take is lowered when many pages are asked for. Continue a long page with get_page from its nextOffset. "
        + "Slugs that are not found come back in missing.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "slugs": {
            "type": "array",
            "items": { "type": "string" },
            "description": "Up to 10 page slugs, from list_pages, search_pages or find_in_pages."
          },
          "take": { "type": "integer", "description": "How many characters of each body to read. Defaults to 4000." }
        }
        """,
        "slugs");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var slugs = AiToolSchema.GetStringArray(arguments, "slugs")
            .Select(slug => slug.Trim())
            .Where(slug => slug.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (slugs.Count == 0)
        {
            return AiToolExecution.Failed("At least one slug is required.");
        }

        if (slugs.Count > MaximumPages)
        {
            return AiToolExecution.Failed($"Ask for at most {MaximumPages} pages at a time.");
        }

        var requestedTake = AiToolSchema.GetInt(arguments, "take") ?? DefaultTake;
        var budgetPerPage = TotalBudget / slugs.Count;
        var maximumTake = Math.Min(budgetPerPage, AiPageDetail.MaximumTake);
        var take = Math.Clamp(requestedTake, 1, maximumTake);
        var pages = new List<object>();
        var missing = new List<string>();

        foreach (var slug in slugs)
        {
            var result = await Mediator.Send(new GetPageQuery(slug), cancellationToken);
            var page = result.Payload;
            var isFound = result.IsSuccess && page is not null;

            if (!isFound)
            {
                missing.Add(slug);

                continue;
            }

            pages.Add(AiPageDetail.Read(page!, 0, take));
        }

        var detail = new { pages, missing };
        var content = JsonSerializer.Serialize(detail);

        return AiToolExecution.Success(content);
    }
}
