using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class FindInPagesTool : IAiTool
{
    private const int DefaultPageSize = 25;
    private const int MaximumPageSize = 100;
    private const int DefaultMatchesPerPage = 5;
    private const int MaximumMatchesPerPage = 20;

    private readonly IMediator Mediator;

    public FindInPagesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "find_in_pages";

    public string Description =>
        "Scan the markdown body of every live page, or every page inside one folder, for a literal substring "
        + "or a regular expression. Unlike search_pages it is exhaustive and exact, not ranked by relevance. "
        + AiTextMatcher.RegexDescription
        + "Returns each matching page's id, slug, name, path and matchCount, with the line number and text of its "
        + "first matches. pagesMatched counts every matching page; ask for the next page until hasMore is false. "
        + "omittedForSize counts matching pages on this page that did not fit in the result; ask again with a "
        + "smaller pageSize or matchesPerPage to see them.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "pattern": { "type": "string", "description": "The text, or the regular expression when regex is true." },
          "regex": { "type": "boolean", "description": "Treat pattern as a regular expression. Defaults to false." },
          "caseSensitive": { "type": "boolean", "description": "Match case exactly. Defaults to false." },
          "folderId": { "type": "integer", "description": "Only scan pages inside this page or folder, at any depth." },
          "page": { "type": "integer", "description": "Which page of results to return, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many matching pages to return per page, up to 100. Defaults to 25." },
          "matchesPerPage": { "type": "integer", "description": "How many matches to show per page, up to 20. Defaults to 5." }
        }
        """,
        "pattern");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var pattern = AiToolSchema.GetString(arguments, "pattern");
        var hasPattern = !string.IsNullOrEmpty(pattern);

        if (!hasPattern)
        {
            return AiToolExecution.Failed("A pattern is required.");
        }

        var isRegex = AiToolSchema.GetBool(arguments, "regex") ?? false;
        var isCaseSensitive = AiToolSchema.GetBool(arguments, "caseSensitive") ?? false;
        var matcher = AiTextMatcher.Create(pattern!, isRegex, isCaseSensitive);

        if (matcher is null)
        {
            return AiToolExecution.Failed(AiTextMatcher.InvalidMessage(pattern!));
        }

        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var folderId = AiToolSchema.GetInt(arguments, "folderId");
        var isUnknownFolder = folderId is { } id && tree.All(node => node.Id != id);

        if (isUnknownFolder)
        {
            return AiToolExecution.Failed($"Page {folderId} is not a live page in this workspace.");
        }

        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var page = Math.Max(AiToolSchema.GetInt(arguments, "page") ?? 1, 1);
        var requestedMatchesPerPage = AiToolSchema.GetInt(arguments, "matchesPerPage") ?? DefaultMatchesPerPage;
        var matchesPerPage = Math.Clamp(requestedMatchesPerPage, 1, MaximumMatchesPerPage);
        var scopeIds = folderId.HasValue
            ? AiPageLookup.InTreeOrder(tree, folderId).Select(node => node.Id).ToList()
            : null;

        var search = new PageTextSearch
        {
            Pattern = new TextPattern(pattern!, isRegex, isCaseSensitive),
            ScopeIds = scopeIds,
            Page = page,
            PageSize = pageSize,
        };

        var result = await Mediator.Send(new FindPagesByTextQuery(search), cancellationToken);

        var hasPages = result.IsSuccess && result.Payload is not null;

        if (!hasPages)
        {
            return AiToolExecution.Failed(result.Message ?? "Pages could not be searched.");
        }

        var found = result.Payload!;
        var nodesById = tree.ToDictionary(node => node.Id);
        var paths = AiPageLookup.Paths(tree);
        var context = new PageContext
        {
            NodesById = nodesById,
            Paths = paths,
            Matcher = matcher,
            MatchesPerPage = matchesPerPage,
        };
        List<object> rows;

        try
        {
            rows = found.Items.Select(body => DescribePage(body, context)).ToList();
        }
        catch (RegexMatchTimeoutException)
        {
            return AiToolExecution.Failed(AiTextMatcher.TimeoutMessage);
        }

        var hasMore = page * pageSize < found.TotalCount;
        var fitted = AiResultBudget.Fit(rows);
        var listing = new JsonObject
        {
            ["pagesMatched"] = found.TotalCount,
            ["page"] = page,
            ["hasMore"] = hasMore,
            ["pages"] = fitted.Rows,
        };

        if (fitted.Omitted > 0)
        {
            listing["omittedForSize"] = fitted.Omitted;
        }

        return AiToolExecution.Success(listing.ToJsonString());
    }

    private static object DescribePage(PageBodyViewModel body, PageContext context)
    {
        var node = context.NodesById.GetValueOrDefault(body.Id);
        var matches = AiTextMatcher.Find(context.Matcher, body.Body, context.MatchesPerPage);

        return new
        {
            id = body.Id,
            slug = node?.Slug,
            name = node?.Title,
            path = context.Paths.GetValueOrDefault(body.Id),
            matchCount = matches.Count,
            matches = matches.Shown.Select(match => new { line = match.Line, text = match.Text }),
        };
    }

    private sealed record PageContext
    {
        public required Dictionary<int, PageTreeNodeViewModel> NodesById { get; init; }

        public required Dictionary<int, string?> Paths { get; init; }

        public required Regex Matcher { get; init; }

        public int MatchesPerPage { get; init; }
    }
}
