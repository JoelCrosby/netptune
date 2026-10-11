using System.Text.Json;

using Mediator;

using Netptune.Ai.Web;
using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetPageTool : IAiTool
{
    private const int DefaultTake = 8000;
    private const int MaximumTake = 20000;

    private readonly IMediator Mediator;

    public GetPageTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_page";

    public string Description =>
        "Read one page by its slug: title, kind, status, tags, revision and its body as markdown. "
        + "Long bodies come in parts; continue from nextOffset until hasMore is false. "
        + "Mentions in the body look like [[task:NPT-42|name]], [[page:12|name]], [[project:3|name]], "
        + "[[sprint:7|name]] or [[board:5|name]].";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "slug": { "type": "string", "description": "The page slug, from search_pages or the viewing block." },
          "offset": { "type": "integer", "description": "Character offset in the body to read from. Defaults to 0." },
          "take": { "type": "integer", "description": "How many characters of body to read, up to 20000. Defaults to 8000." }
        }
        """,
        "slug");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var slug = AiToolSchema.GetString(arguments, "slug")?.Trim();
        var hasSlug = !string.IsNullOrWhiteSpace(slug);

        if (!hasSlug)
        {
            return AiToolExecution.Failed("A slug is required.");
        }

        var result = await Mediator.Send(new GetPageQuery(slug!), cancellationToken);
        var page = result.Payload;

        var isFound = result.IsSuccess && page is not null;

        if (!isFound)
        {
            return AiToolExecution.Failed($"Page {slug} was not found in this workspace.");
        }

        var requestedTake = AiToolSchema.GetInt(arguments, "take") ?? DefaultTake;
        var take = Math.Clamp(requestedTake, 1, MaximumTake);
        var offset = AiToolSchema.GetInt(arguments, "offset") ?? 0;
        var body = WebDocumentPage.Read(page!.Body ?? string.Empty, offset, take);

        var detail = new
        {
            id = page.Id,
            slug = page.Slug,
            name = page.Title,
            kind = page.Kind.ToString(),
            status = page.Status.ToString(),
            parentId = page.ParentId,
            tags = page.Tags.Select(tag => tag.Name),
            revision = page.Revision,
            updatedAt = page.UpdatedAt ?? page.CreatedAt,
            updatedBy = page.ModifiedByDisplayName ?? page.CreatedByDisplayName,
            totalCharacters = page.Body?.Length ?? 0,
            offset = body.Offset,
            nextOffset = body.NextOffset,
            hasMore = body.HasMore,
            body = body.Text,
        };

        var content = JsonSerializer.Serialize(detail);

        return AiToolExecution.Success(content);
    }
}
