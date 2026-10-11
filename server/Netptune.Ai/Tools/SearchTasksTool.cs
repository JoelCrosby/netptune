using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.Utilities;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Search.Queries;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class SearchTasksTool : IAiTool
{
    private const int DefaultPageSize = 25;
    private const int MaximumPageSize = 100;
    private const int SearchCandidateLimit = 100;
    private const int ExcerptLength = 160;

    // Below Ai:MaxToolResultCharacters, so the runner never cuts a result off mid-JSON.
    private const int ResultCharacterBudget = 28_000;

    private static readonly JsonSerializerOptions RowOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IMediator Mediator;

    public SearchTasksTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "search_tasks";

    public string Description =>
        "Search tasks in the current workspace. search matches words in task names and descriptions, ranked by relevance; "
        + "a system id such as NPT-42 finds that task. Filter by project, sprint, status, assignee, tags or board column. "
        + "Each task comes back with its status, priority, assignees, tags, sprint, the board columns it sits in, flags, "
        + "estimate, dates and the start of its description. Use page to read past the first page.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "search": { "type": "string", "description": "Words to find in task names and descriptions, or a system id such as NPT-42." },
          "projectId": { "type": "integer", "description": "Restrict results to a single project." },
          "sprintId": { "type": "integer", "description": "Restrict results to a single sprint." },
          "statusId": { "type": "integer", "description": "Restrict results to a single status, from list_records." },
          "assigneeId": { "type": "string", "description": "Restrict results to one assignee, using a userId from list_records members." },
          "hasAssignee": { "type": "boolean", "description": "True for tasks with at least one assignee, false for tasks nobody is assigned to." },
          "noSprint": { "type": "boolean", "description": "Only tasks that are not in any sprint." },
          "hasFlags": { "type": "boolean", "description": "Only tasks carrying a flag." },
          "hasTags": { "type": "boolean", "description": "True for tasks carrying at least one tag, false for tasks with no tags at all." },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "Only tasks carrying at least one of these tag names, from list_records."
          },
          "boardGroupId": { "type": "integer", "description": "Only tasks in this board column, from list_records board_groups." },
          "notInBoardGroupId": { "type": "integer", "description": "Only tasks that are not in this board column." },
          "page": { "type": "integer", "description": "Which page of results to return, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many tasks to return per page, up to 100." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var page = Math.Max(AiToolSchema.GetInt(arguments, "page") ?? 1, 1);
        var requestedTags = AiTagVocabulary.ReadRequested(arguments);
        var resolvedTags = await ResolveTags(requestedTags, cancellationToken);

        if (resolvedTags.Unknown.Count > 0)
        {
            return AiToolExecution.Failed(
                $"These tags do not exist in this workspace: {string.Join(", ", resolvedTags.Unknown)}. List the tags with list_records to see the available names.");
        }

        var criteria = ReadCriteria(arguments, resolvedTags.Matched);
        var search = AiToolSchema.GetString(arguments, "search")?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var usesSearchIndex = hasSearch && !IsTaskReference(search!);

        if (usesSearchIndex)
        {
            var ranked = await SearchByRelevance(search!, criteria, page, pageSize, cancellationToken);

            if (ranked is not null)
            {
                return ranked;
            }
        }

        return await SearchByFilters(search, criteria, page, pageSize, cancellationToken);
    }

    // Free text goes to the search index for ranking only; every filter is still applied by the task query.
    private async Task<AiToolExecution?> SearchByRelevance(
        string search,
        TaskCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var indexQuery = new SearchTaskIdsQuery(search, SearchCandidateLimit, criteria.ProjectId, criteria.AssigneeId);
        var candidates = await Mediator.Send(indexQuery, cancellationToken);
        var hasCandidates = candidates is { TaskIds.Count: > 0 };

        if (!hasCandidates)
        {
            return null;
        }

        var filter = criteria.ToFilter(search: null, candidates!.TaskIds, page: 1, candidates.TaskIds.Count);
        var result = await Mediator.Send(new GetTasksQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return AiToolExecution.Failed(result.Message ?? "Tasks could not be read.");
        }

        var rankById = candidates.TaskIds
            .Select((taskId, rank) => (taskId, rank))
            .ToDictionary(item => item.taskId, item => item.rank);

        var matched = (result.Payload?.Items ?? [])
            .OrderBy(task => rankById.GetValueOrDefault(task.Id, int.MaxValue))
            .ToList();

        var pageOfTasks = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var moreInIndex = candidates.EstimatedTotalHits > candidates.TaskIds.Count;

        return Render("relevance", matched.Count, page, pageOfTasks, moreInIndex);
    }

    private async Task<AiToolExecution> SearchByFilters(
        string? search,
        TaskCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filter = criteria.ToFilter(search, taskIds: [], page, pageSize);
        var result = await Mediator.Send(new GetTasksQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return AiToolExecution.Failed(result.Message ?? "Tasks could not be read.");
        }

        var tasks = result.Payload?.Items ?? [];
        var totalCount = result.Payload?.TotalCount ?? 0;
        var mode = string.IsNullOrWhiteSpace(search) ? "filters" : "name";

        return Render(mode, totalCount, page, tasks, moreInIndex: false);
    }

    private static AiToolExecution Render(
        string mode,
        int totalCount,
        int page,
        IReadOnlyList<TaskViewModel> tasks,
        bool moreInIndex)
    {
        var rows = new JsonArray();
        var usedCharacters = 0;

        foreach (var task in tasks)
        {
            var row = JsonSerializer.SerializeToNode(ToRow(task), RowOptions)!;
            var rowCharacters = row.ToJsonString().Length;
            var exceedsBudget = usedCharacters + rowCharacters > ResultCharacterBudget;

            if (exceedsBudget)
            {
                break;
            }

            usedCharacters += rowCharacters;
            rows.Add(row);
        }

        var omitted = tasks.Count - rows.Count;
        var content = new JsonObject
        {
            ["searchMode"] = mode,
            ["totalCount"] = totalCount,
            ["page"] = page,
            ["returned"] = rows.Count,
        };

        if (omitted > 0)
        {
            content["omittedForSize"] = omitted;
        }

        if (moreInIndex)
        {
            content["moreMatchesBeyondTop100"] = true;
        }

        content["tasks"] = rows;

        return AiToolExecution.Success(content.ToJsonString());
    }

    private static object ToRow(TaskViewModel task)
    {
        var columns = task.Placements.Select(placement => new
        {
            board = placement.BoardName,
            column = placement.BoardGroupName,
            boardGroupId = placement.BoardGroupId,
        });

        var flags = task.Flags.Select(flag => flag.Name).ToList();
        var assignees = task.Assignees.Select(assignee => assignee.DisplayName).ToList();
        var hasEstimate = task.EstimateValue.HasValue && task.EstimateType.HasValue;

        return new
        {
            taskId = task.Id,
            systemId = task.SystemId,
            name = task.Name,
            status = task.StatusName,
            statusCategory = task.StatusCategory.ToString(),
            priority = task.Priority?.ToString(),
            assignees = assignees.Count > 0 ? assignees : null,
            tags = task.Tags.Count > 0 ? task.Tags : null,
            projectId = task.ProjectId,
            project = task.ProjectName,
            sprintId = task.SprintId,
            sprint = task.SprintName,
            columns,
            flags = flags.Count > 0 ? flags : null,
            estimate = hasEstimate ? $"{task.EstimateValue} {task.EstimateType}" : null,
            startDate = task.StartDate,
            dueDate = task.DueDate,
            updated = DateOnly.FromDateTime((task.UpdatedAt ?? task.CreatedAt).UtcDateTime),
            excerpt = Excerpt(task.Description),
        };
    }

    private static string? Excerpt(string? description)
    {
        var text = RichTextExtractor.ToPlainText(description)?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.Length <= ExcerptLength)
        {
            return text;
        }

        return $"{text[..ExcerptLength].TrimEnd()}…";
    }

    private static bool IsTaskReference(string search)
    {
        var isScopeId = int.TryParse(search, out _);
        var separatorIndex = search.LastIndexOf('-');
        var hasIdentifierShape = separatorIndex > 0
            && separatorIndex < search.Length - 1
            && !search.Contains(' ')
            && int.TryParse(search[(separatorIndex + 1)..], out _);

        return isScopeId || hasIdentifierShape;
    }

    private static TaskCriteria ReadCriteria(JsonElement arguments, List<string> tags)
    {
        var statusId = AiToolSchema.GetInt(arguments, "statusId");
        var assigneeId = AiToolSchema.GetString(arguments, "assigneeId")?.Trim();
        var hasAssigneeId = !string.IsNullOrWhiteSpace(assigneeId);

        return new TaskCriteria
        {
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            SprintId = AiToolSchema.GetInt(arguments, "sprintId"),
            StatusIds = statusId.HasValue ? [statusId.Value] : [],
            AssigneeId = hasAssigneeId ? assigneeId : null,
            Tags = tags,
            NoSprint = AiToolSchema.GetBool(arguments, "noSprint"),
            HasFlags = AiToolSchema.GetBool(arguments, "hasFlags"),
            HasTags = AiToolSchema.GetBool(arguments, "hasTags"),
            HasAssignee = AiToolSchema.GetBool(arguments, "hasAssignee"),
            BoardGroupId = AiToolSchema.GetInt(arguments, "boardGroupId"),
            NotInBoardGroupId = AiToolSchema.GetInt(arguments, "notInBoardGroupId"),
        };
    }

    private async Task<TagResolution> ResolveTags(List<string> requested, CancellationToken cancellationToken)
    {
        if (requested.Count == 0)
        {
            return new TagResolution([], []);
        }

        var workspaceTags = await Mediator.Send(new GetTagsByNameQuery(requested), cancellationToken);
        var knownNames = workspaceTags.Select(tag => tag.Name).ToList();
        var matched = new List<string>();
        var unknown = new List<string>();

        foreach (var name in requested)
        {
            var known = knownNames.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));

            if (known is null)
            {
                unknown.Add(name);
                continue;
            }

            matched.Add(known);
        }

        return new TagResolution(matched, unknown);
    }

    private sealed record TagResolution(List<string> Matched, List<string> Unknown);

    private sealed record TaskCriteria
    {
        public int? ProjectId { get; init; }

        public int? SprintId { get; init; }

        public int[] StatusIds { get; init; } = [];

        public string? AssigneeId { get; init; }

        public List<string> Tags { get; init; } = [];

        public bool? NoSprint { get; init; }

        public bool? HasFlags { get; init; }

        public bool? HasTags { get; init; }

        public bool? HasAssignee { get; init; }

        public int? BoardGroupId { get; init; }

        public int? NotInBoardGroupId { get; init; }

        public TaskFilter ToFilter(string? search, IReadOnlyList<int> taskIds, int page, int pageSize)
        {
            return new TaskFilter
            {
                Search = search,
                TaskIds = [.. taskIds],
                ProjectId = ProjectId,
                SprintId = SprintId,
                StatusIds = StatusIds,
                Assignees = AssigneeId is null ? [] : [AssigneeId],
                Tags = [.. Tags],
                NoSprint = NoSprint,
                HasFlags = HasFlags,
                HasTags = HasTags,
                HasAssignee = HasAssignee,
                BoardGroupId = BoardGroupId,
                NotInBoardGroupId = NotInBoardGroupId,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}
