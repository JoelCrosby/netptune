using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
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

    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 100;
    private const string StableSort = "name";

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
        "List one kind of workspace record, a page at a time: statuses, tags, projects, relation_types "
        + "(how tasks can be linked, such as blocks), board_groups (board columns, with their board and project; "
        + "boardId narrows them to one board), boards, sprints (the active sprint first, then the newest; projectId "
        + "narrows them to one project), members (the people work can be assigned to) and member_roles (each member's "
        + "role and pending invites, for who owns or administers the workspace). "
        + "Projects and boards come in name order. search narrows any kind by name. "
        + "Every kind returns totalCount, page, hasMore and records; ask for the "
        + "next page until hasMore is false to see them all. omittedForSize counts records on this page that did "
        + "not fit; ask again with a smaller pageSize.";

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
          "search": { "type": "string", "description": "A name fragment to narrow the records by." },
          "projectId": { "type": "integer", "description": "For sprints: restrict to one project." },
          "boardId": { "type": "integer", "description": "For board_groups: restrict to one board." },
          "page": { "type": "integer", "description": "Which page of records to return, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many records to return per page, up to 100. Defaults to 50." }
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
        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var query = new RecordQuery
        {
            Page = Math.Max(AiToolSchema.GetInt(arguments, "page") ?? 1, 1),
            PageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize),
            Search = AiToolSchema.GetString(arguments, "search")?.Trim(),
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            BoardId = AiToolSchema.GetInt(arguments, "boardId"),
        };

        return kind switch
        {
            Statuses => await ListStatuses(query, cancellationToken),
            Tags => await ListTags(query, cancellationToken),
            Projects => await ListProjects(query, cancellationToken),
            RelationTypes => await ListRelationTypes(query, cancellationToken),
            BoardGroups => await ListBoardGroups(query, cancellationToken),
            Boards => await ListBoards(query, cancellationToken),
            Sprints => await ListSprints(query, cancellationToken),
            Members => await ListMembers(query, cancellationToken),
            MemberRoles => await ListMemberRoles(query, cancellationToken),
            _ => AiToolExecution.Failed($"kind must be one of: {string.Join(", ", PermissionsByKind.Keys)}."),
        };
    }

    private async Task<AiToolExecution> ListStatuses(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new StatusPageFilter { Page = query.Page, PageSize = query.PageSize, Search = query.Search };
        var result = await Mediator.Send(new GetStatusesPageQuery(filter), cancellationToken);

        return Render(result, "Statuses could not be read.", status => new
        {
            id = status.Id,
            name = status.Name,
            category = status.Category.ToString(),
            color = status.Color,
            description = status.Description,
        });
    }

    private async Task<AiToolExecution> ListTags(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new TagFilter { Page = query.Page, PageSize = query.PageSize, Search = query.Search };
        var result = await Mediator.Send(new GetTagsPageQuery(filter), cancellationToken);

        return Render(result, "Tags could not be read.", tag => new
        {
            id = tag.Id,
            name = tag.Name,
        });
    }

    private async Task<AiToolExecution> ListProjects(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new ProjectFilter
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            SortBy = StableSort,
        };

        var result = await Mediator.Send(new GetProjectsPageQuery(filter), cancellationToken);

        return Render(result, "Projects could not be read.", project => new
        {
            id = project.Id,
            name = project.Name,
            key = project.Key,
            repositoryUrl = project.RepositoryUrl,
        });
    }

    private async Task<AiToolExecution> ListRelationTypes(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new RelationTypeFilter { Page = query.Page, PageSize = query.PageSize, Search = query.Search };
        var result = await Mediator.Send(new GetRelationTypesPageQuery(filter), cancellationToken);

        return Render(result, "Relation types could not be read.", relationType => new
        {
            id = relationType.Id,
            name = relationType.Name,
            inverseName = relationType.InverseName,
            category = relationType.Category.ToString(),
        });
    }

    private async Task<AiToolExecution> ListBoardGroups(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new BoardGroupOptionFilter
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            BoardId = query.BoardId,
        };

        var result = await Mediator.Send(new GetBoardGroupOptionsPageQuery(filter), cancellationToken);

        return Render(result, "Board groups could not be read.", option => new
        {
            id = option.Id,
            name = option.Name,
            boardId = option.BoardId,
            boardName = option.BoardName,
            boardIdentifier = option.BoardIdentifier,
            projectName = option.ProjectName,
        });
    }

    private async Task<AiToolExecution> ListBoards(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new BoardFilter
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            SortBy = StableSort,
        };

        var result = await Mediator.Send(new GetBoardsInWorkspaceQuery(filter), cancellationToken);

        return Render(result, "Boards could not be read.", board => new
        {
            id = board.Id,
            name = board.Name,
            identifier = board.Identifier,
            boardType = board.BoardType.ToString(),
            projectId = board.ProjectId,
            projectName = board.ProjectName,
            taskCount = board.TaskCount,
        });
    }

    private async Task<AiToolExecution> ListSprints(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new SprintPageFilter
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            ProjectId = query.ProjectId,
        };

        var result = await Mediator.Send(new GetSprintsPageQuery(filter), cancellationToken);

        return Render(result, "Sprints could not be read.", sprint => new
        {
            id = sprint.Id,
            name = sprint.Name,
            identifier = sprint.Identifier,
            status = sprint.Status.ToString(),
            projectId = sprint.ProjectId,
            startDate = sprint.StartDate,
            endDate = sprint.EndDate,
        });
    }

    private async Task<AiToolExecution> ListMembers(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new AssigneeFilter { Page = query.Page, PageSize = query.PageSize, Search = query.Search };
        var result = await Mediator.Send(new GetAssigneesQuery(filter), cancellationToken);

        return Render(result, "Members could not be read.", member => new
        {
            id = member.Id,
            name = member.DisplayName,
        });
    }

    private async Task<AiToolExecution> ListMemberRoles(RecordQuery query, CancellationToken cancellationToken)
    {
        var filter = new WorkspaceUserFilter { Page = query.Page, PageSize = query.PageSize, Search = query.Search };
        var result = await Mediator.Send(new GetWorkspaceUsersQuery(filter), cancellationToken);

        return Render(result, "Members could not be read.", member => new
        {
            id = member.Id,
            name = member.DisplayName,
            email = member.Email,
            role = member.Role.ToString(),
            isPending = member.IsPending,
        });
    }

    private static AiToolExecution Render<T>(
        ClientResponse<PagedResponse<T>> result,
        string failureMessage,
        Func<T, object> toRecord)
    {
        var hasPage = result.IsSuccess && result.Payload is not null;

        if (!hasPage)
        {
            return AiToolExecution.Failed(result.Message ?? failureMessage);
        }

        var page = result.Payload!;
        var hasMore = page.Page * page.PageSize < page.TotalCount;
        var fitted = AiResultBudget.Fit(page.Items.Select(toRecord));
        var listing = new JsonObject
        {
            ["totalCount"] = page.TotalCount,
            ["page"] = page.Page,
            ["hasMore"] = hasMore,
            ["records"] = fitted.Rows,
        };

        if (fitted.Omitted > 0)
        {
            listing["omittedForSize"] = fitted.Omitted;
        }

        return AiToolExecution.Success(listing.ToJsonString());
    }

    private sealed record RecordQuery
    {
        public int Page { get; init; }

        public int PageSize { get; init; }

        public string? Search { get; init; }

        public int? ProjectId { get; init; }

        public int? BoardId { get; init; }
    }
}
