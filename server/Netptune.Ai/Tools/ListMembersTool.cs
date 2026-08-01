using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListMembersTool : IAiTool
{
    private const int DefaultPageSize = 50;

    private readonly IMediator Mediator;

    public ListMembersTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_members";

    public string Description =>
        "List workspace members who can be assigned to tasks, with their id and display name.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Members.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "search": { "type": "string", "description": "Optional name fragment to filter members by." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var filter = new AssigneeFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            Page = 1,
            PageSize = DefaultPageSize,
        };

        var result = await Mediator.Send(new GetAssigneesQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return AiToolExecution.Failed(result.Message ?? "Members could not be read.");
        }

        var members = result.Payload?.Items ?? [];
        var summaries = members.Select(member => new
        {
            id = member.Id,
            name = member.DisplayName,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
