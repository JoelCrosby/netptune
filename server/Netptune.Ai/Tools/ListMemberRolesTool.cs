using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListMemberRolesTool : IAiTool
{
    private const int DefaultPageSize = 50;

    private readonly IMediator Mediator;

    public ListMemberRolesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_member_roles";

    public string Description =>
        "List workspace members with their role and whether an invite is still pending. "
        + "Use this for questions about who owns or administers the workspace, or who has not accepted an invite. "
        + "For picking someone to assign work to, use list_members instead.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Members.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var page = new PageRequest { Page = 1, PageSize = DefaultPageSize };
        var result = await Mediator.Send(new GetWorkspaceUsersQuery(page), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "Members could not be read.");
        }

        var members = result.Payload.Items.Select(member => new
        {
            id = member.Id,
            name = member.DisplayName,
            email = member.Email,
            role = member.Role.ToString(),
            isPending = member.IsPending,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(members));
    }
}
