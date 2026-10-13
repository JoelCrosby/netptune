using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class ListPagesTool : IAiTool
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 100;

    private readonly IMediator Mediator;

    public ListPagesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_pages";

    public string Description =>
        "List every live page in the workspace, or every page inside one folder, in sidebar order without needing "
        + "a search term: id, slug, name, kind, status, folder path, characters and format. "
        + "Use it to sweep the whole workspace with certainty; ask for the next page until hasMore is false. "
        + "omittedForSize counts pages on this page that did not fit in the result; ask again with a smaller pageSize. "
        + AiTextFormatJson.Description;

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "folderId": { "type": "integer", "description": "Only list pages inside this page or folder, at any depth." },
          "page": { "type": "integer", "description": "Which page of results to return, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many pages to return per page, up to 100. Defaults to 50." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
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
        var skipped = (page - 1) * pageSize;

        var nodes = AiPageLookup.InTreeOrder(tree, folderId);
        var pageOfNodes = nodes.Skip(skipped).Take(pageSize).ToList();
        var hasMore = skipped + pageSize < nodes.Count;
        var pageIds = pageOfNodes.Select(node => node.Id).ToList();
        var bodies = await AiPageLookup.Bodies(Mediator, pageIds, cancellationToken);
        var paths = AiPageLookup.Paths(tree);

        var rows = pageOfNodes.Select(node =>
        {
            var body = bodies.GetValueOrDefault(node.Id, string.Empty);
            var format = AiTextFormatReader.Read(body);

            return new
            {
                id = node.Id,
                slug = node.Slug,
                name = node.Title,
                kind = node.Kind.ToString().ToLowerInvariant(),
                status = AiPageLookup.DescribeStatus(node.Status),
                path = paths.GetValueOrDefault(node.Id),
                characters = format.Characters,
                format = AiTextFormatJson.From(format),
            };
        });

        var fitted = AiResultBudget.Fit(rows);
        var listing = new JsonObject
        {
            ["totalCount"] = nodes.Count,
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
}
