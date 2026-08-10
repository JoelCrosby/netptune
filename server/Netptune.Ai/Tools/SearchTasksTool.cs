using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class SearchTasksTool : IAiTool
{
    private const int DefaultPageSize = 25;
    private const int MaximumPageSize = 100;

    private readonly IMediator Mediator;

    public SearchTasksTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "search_tasks";

    public string Description =>
        "Search tasks in the current workspace. Filter by free text, project, sprint, status, assignee or tags. "
        + "Use hasTags false to find untagged tasks, and hasAssignee false to find unassigned tasks. "
        + "Returns task id, name, status, assignee, tags and dates.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "search": { "type": "string", "description": "Free text to match against task name and description." },
          "projectId": { "type": "integer", "description": "Restrict results to a single project." },
          "sprintId": { "type": "integer", "description": "Restrict results to a single sprint." },
          "statusId": { "type": "integer", "description": "Restrict results to a single status, from list_statuses." },
          "assigneeId": { "type": "string", "description": "Restrict results to one assignee, using a userId from list_members." },
          "hasAssignee": { "type": "boolean", "description": "True for tasks with at least one assignee, false for tasks nobody is assigned to." },
          "noSprint": { "type": "boolean", "description": "Only tasks that are not in any sprint." },
          "hasFlags": { "type": "boolean", "description": "Only tasks carrying a flag." },
          "hasTags": { "type": "boolean", "description": "True for tasks carrying at least one tag, false for tasks with no tags at all." },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "Only tasks carrying at least one of these tag names, from list_tags."
          },
          "pageSize": { "type": "integer", "description": "How many tasks to return, up to 100." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var statusId = AiToolSchema.GetInt(arguments, "statusId");
        var assigneeId = AiToolSchema.GetString(arguments, "assigneeId")?.Trim();
        var hasAssigneeId = !string.IsNullOrWhiteSpace(assigneeId);
        var requestedTags = AiTagVocabulary.ReadRequested(arguments);
        var resolvedTags = await ResolveTags(requestedTags, cancellationToken);

        if (resolvedTags is null)
        {
            return AiToolExecution.Failed("Workspace tags could not be read.");
        }

        if (resolvedTags.Unknown.Count > 0)
        {
            return AiToolExecution.Failed(
                $"These tags do not exist in this workspace: {string.Join(", ", resolvedTags.Unknown)}. Use list_tags to see the available names.");
        }

        var filter = new TaskFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            SprintId = AiToolSchema.GetInt(arguments, "sprintId"),
            StatusIds = statusId.HasValue ? [statusId.Value] : [],
            Assignees = hasAssigneeId ? [assigneeId!] : [],
            Tags = [.. resolvedTags.Matched],
            NoSprint = AiToolSchema.GetBool(arguments, "noSprint"),
            HasFlags = AiToolSchema.GetBool(arguments, "hasFlags"),
            HasTags = AiToolSchema.GetBool(arguments, "hasTags"),
            HasAssignee = AiToolSchema.GetBool(arguments, "hasAssignee"),
            Page = 1,
            PageSize = pageSize,
        };

        var result = await Mediator.Send(new GetTasksQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return AiToolExecution.Failed(result.Message ?? "Tasks could not be read.");
        }

        var tasks = result.Payload?.Items ?? [];
        var summaries = tasks.Select(task => new
        {
            id = task.Id,
            systemId = task.SystemId,
            name = task.Name,
            status = task.StatusName,
            assignees = task.Assignees.Select(assignee => assignee.DisplayName),
            tags = task.Tags,
            projectId = task.ProjectId,
            projectName = task.ProjectName,
            sprintId = task.SprintId,
            dueDate = task.DueDate,
            priority = task.Priority?.ToString(),
        });

        var content = JsonSerializer.Serialize(new
        {
            totalCount = result.Payload?.TotalCount ?? 0,
            returned = tasks.Count,
            tasks = summaries,
        });

        return AiToolExecution.Success(content);
    }

    private async Task<TagResolution?> ResolveTags(List<string> requested, CancellationToken cancellationToken)
    {
        if (requested.Count == 0)
        {
            return new TagResolution([], []);
        }

        var workspaceTags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        if (workspaceTags is null)
        {
            return null;
        }

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
}
