using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetPageTool : IAiTool
{
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
        + "[[sprint:7|name]] or [[board:5|name]]. "
        + AiTextFormatJson.Description;

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Pages.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "slug": { "type": "string", "description": "The page slug, from list_pages, search_pages or the viewing block." },
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

        var requestedTake = AiToolSchema.GetInt(arguments, "take") ?? AiPageDetail.DefaultTake;
        var take = Math.Clamp(requestedTake, 1, AiPageDetail.MaximumTake);
        var offset = AiToolSchema.GetInt(arguments, "offset") ?? 0;
        var detail = AiPageDetail.Read(page!, offset, take);
        var content = JsonSerializer.Serialize(detail);

        return AiToolExecution.Success(content);
    }
}
