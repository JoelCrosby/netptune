using System.Text.Json;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Web;
using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Ai.Queries;

namespace Netptune.Ai.Tools;

public sealed class ReadWebDocumentTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly AiWebOptions Options;

    public ReadWebDocumentTool(IMediator mediator, IOptions<AiOptions> options)
    {
        Mediator = mediator;
        Options = options.Value.Web;
    }

    public string Name => "read_web_document";

    public string Description =>
        "Read the next part of a page already fetched with web_fetch, using the documentId it returned. "
        + "Continue from nextOffset until hasMore is false.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Assistant.UseWeb };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "documentId": { "type": "string", "description": "The documentId web_fetch returned." },
          "offset": { "type": "integer", "description": "Character offset to read from. Defaults to 0." },
          "take": { "type": "integer", "description": "How many characters to read. Defaults to 6000." }
        }
        """,
        "documentId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var rawId = AiToolSchema.GetString(arguments, "documentId")?.Trim();
        var isId = Guid.TryParse(rawId, out var documentId);

        if (!isId)
        {
            return AiToolExecution.Failed("A documentId from web_fetch is required.");
        }

        var document = await Mediator.Send(new GetAiWebDocumentQuery(documentId), cancellationToken);

        if (document is null)
        {
            return AiToolExecution.Failed("That document has expired or is not in this workspace. Fetch the page again.");
        }

        var requestedTake = AiToolSchema.GetInt(arguments, "take") ?? Options.DefaultPageCharacters;
        var take = Math.Clamp(requestedTake, 1, Options.MaxPageCharacters);
        var offset = AiToolSchema.GetInt(arguments, "offset") ?? 0;
        var page = WebDocumentPage.Read(document.Content, offset, take);

        var summary = new
        {
            documentId = document.Id,
            url = document.FinalUrl,
            title = document.Title,
            totalCharacters = document.CharacterCount,
            offset = page.Offset,
            nextOffset = page.NextOffset,
            hasMore = page.HasMore,
            content = page.Text,
        };

        var content = JsonSerializer.Serialize(summary);

        return AiToolExecution.Success(content);
    }
}
