using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Boards.Queries;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.RelationTypes.Queries;
using Netptune.Handlers.Sprints.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListRecordsTool : IAiTool
{
    public const string ToolName = "list_records";

    public const string Statuses = "statuses";
    public const string Tags = "tags";
    public const string Projects = "projects";
    public const string RelationTypes = "relation_types";
    public const string BoardGroups = "board_groups";
    public const string Boards = "boards";
    public const string Sprints = "sprints";
    public const string Members = "members";
    public const string MemberRoles = "member_roles";

    private const int MemberPageSize = 50;
    private const int SprintTake = 25;

    private static readonly Dictionary<string, string> PermissionsByKind = new(StringComparer.Ordinal)
    {
        [Statuses] = NetptunePermissions.Statuses.Read,
        [Tags] = NetptunePermissions.Tags.Read,
        [Projects] = NetptunePermissions.Projects.Read,
        [RelationTypes] = NetptunePermissions.RelationTypes.Read,
        [BoardGroups] = NetptunePermissions.BoardGroups.Read,
        [Boards] = NetptunePermissions.Boards.Read,
        [Sprints] = NetptunePermissions.Sprints.Read,
        [Members] = NetptunePermissions.Members.Read,
        [MemberRoles] = NetptunePermissions.Members.Read,
    };

    private readonly IMediator Mediator;

    public ListRecordsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => ToolName;

    public string Description =>
        "List one kind of workspace record. "
        + "statuses, tags, projects, relation_types (how tasks can be linked, such as blocks) "
        + "and board_groups (board columns, with their board and project) return everything. "
        + "boards returns at most 100; search narrows them. "
        + "sprints returns the active sprint first, then the newest, 25 at most, optionally for one projectId. "
        + "members are the people work can be assigned to; search filters them by name. "
        + "member_roles adds each member's role and pending invites, for who owns or administers the workspace.";

    public AiToolKind Kind => AiToolKind.Read;

    // Each kind needs only its own read permission, so the tool is offered when any of them is held and
    // every call is checked against the kind it asks for.
    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(PermissionsByKind.Values, StringComparer.Ordinal);

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "kind": {
            "type": "string",
            "enum": ["statuses", "tags", "projects", "relation_types", "board_groups", "boards", "sprints", "members", "member_roles"],
            "description": "Which records to list."
          },
          "search": { "type": "string", "description": "For boards and members: a name fragment to narrow by." },
          "projectId": { "type": "integer", "description": "For sprints: restrict to one project." }
        }
        """,
        "kind");

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return RequiredPermissions.Any(permissions.Contains);
    }

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var kind = AiToolSchema.GetString(arguments, "kind") ?? string.Empty;
        var isKnown = PermissionsByKind.TryGetValue(kind, out var permission);

        // An unknown kind reads nothing, and Execute says which kinds there are.
        return isKnown
            ? new HashSet<string>(StringComparer.Ordinal) { permission! }
            : new HashSet<string>(StringComparer.Ordinal);
    }

    public string DescribeCall(JsonElement arguments)
    {
        return AiToolSchema.DescribeCall(Name, arguments, "kind", PermissionsByKind.Keys);
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var kind = AiToolSchema.GetString(arguments, "kind");

        return kind switch
        {
            Statuses => await ListStatuses(cancellationToken),
            Tags => await ListTags(cancellationToken),
            Projects => await ListProjects(cancellationToken),
            RelationTypes => await ListRelationTypes(cancellationToken),
            BoardGroups => await ListBoardGroups(cancellationToken),
            Boards => await ListBoards(arguments, cancellationToken),
            Sprints => await ListSprints(arguments, cancellationToken),
            Members => await ListMembers(arguments, cancellationToken),
            MemberRoles => await ListMemberRoles(cancellationToken),
            _ => AiToolExecution.Failed($"kind must be one of: {string.Join(", ", PermissionsByKind.Keys)}."),
        };
    }

    private async Task<AiToolExecution> ListStatuses(CancellationToken cancellationToken)
    {
        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);

        if (statuses is null)
        {
            return AiToolExecution.Failed("Statuses could not be read.");
        }

        var summaries = statuses.Select(status => new
        {
            id = status.Id,
            name = status.Name,
            category = status.Category.ToString(),
            color = status.Color,
            description = status.Description,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListTags(CancellationToken cancellationToken)
    {
        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        if (tags is null)
        {
            return AiToolExecution.Failed("Tags could not be read.");
        }

        var names = tags.Select(tag => tag.Name);

        return AiToolExecution.Success(JsonSerializer.Serialize(names));
    }

    private async Task<AiToolExecution> ListProjects(CancellationToken cancellationToken)
    {
        var projects = await Mediator.Send(new GetProjectsQuery(), cancellationToken);
        var summaries = projects.Select(project => new
        {
            id = project.Id,
            name = project.Name,
            key = project.Key,
            repositoryUrl = project.RepositoryUrl,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListRelationTypes(CancellationToken cancellationToken)
    {
        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        if (relationTypes is null)
        {
            return AiToolExecution.Failed("Relation types could not be read.");
        }

        var summaries = relationTypes.Select(relationType => new
        {
            id = relationType.Id,
            name = relationType.Name,
            inverseName = relationType.InverseName,
            category = relationType.Category.ToString(),
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListBoardGroups(CancellationToken cancellationToken)
    {
        var options = await Mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);
        var summaries = options.Select(option => new
        {
            id = option.Id,
            name = option.Name,
            boardName = option.BoardName,
            boardIdentifier = option.BoardIdentifier,
            projectName = option.ProjectName,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListBoards(JsonElement arguments, CancellationToken cancellationToken)
    {
        var filter = new BoardFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            Page = 1,
            PageSize = PaginationDefaults.MaxPageSize,
        };

        var result = await Mediator.Send(new GetBoardsInWorkspaceQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "Boards could not be read.");
        }

        var summaries = result.Payload.Items.Select(board => new
        {
            id = board.Id,
            name = board.Name,
            identifier = board.Identifier,
            boardType = board.BoardType.ToString(),
            projectId = board.ProjectId,
            projectName = board.ProjectName,
            taskCount = board.TaskCount,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListSprints(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.GetInt(arguments, "projectId");
        var sprints = await Mediator.Send(new GetSprintsQuery(projectId, [], SprintTake), cancellationToken);
        var summaries = sprints.Select(sprint => new
        {
            id = sprint.Id,
            name = sprint.Name,
            identifier = sprint.Identifier,
            status = sprint.Status.ToString(),
            projectId = sprint.ProjectId,
            startDate = sprint.StartDate,
            endDate = sprint.EndDate,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListMembers(JsonElement arguments, CancellationToken cancellationToken)
    {
        var filter = new AssigneeFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            Page = 1,
            PageSize = MemberPageSize,
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

        return AiToolExecution.Success(JsonSerializer.Serialize(summaries));
    }

    private async Task<AiToolExecution> ListMemberRoles(CancellationToken cancellationToken)
    {
        var page = new WorkspaceUserFilter { Page = 1, PageSize = MemberPageSize };
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
