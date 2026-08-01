using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
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
        "Search tasks in the current workspace. Filter by free text, project, or sprint. Returns task id, name, status, assignee and dates.";

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
          "noSprint": { "type": "boolean", "description": "Only tasks that are not in any sprint." },
          "hasFlags": { "type": "boolean", "description": "Only tasks carrying a flag." },
          "pageSize": { "type": "integer", "description": "How many tasks to return, up to 100." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var statusId = AiToolSchema.GetInt(arguments, "statusId");
        var assigneeId = AiToolSchema.GetString(arguments, "assigneeId")?.Trim();
        var hasAssignee = !string.IsNullOrWhiteSpace(assigneeId);
        var filter = new TaskFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            ProjectId = AiToolSchema.GetInt(arguments, "projectId"),
            SprintId = AiToolSchema.GetInt(arguments, "sprintId"),
            StatusIds = statusId.HasValue ? [statusId.Value] : [],
            Assignees = hasAssignee ? [assigneeId!] : [],
            NoSprint = AiToolSchema.GetBool(arguments, "noSprint"),
            HasFlags = AiToolSchema.GetBool(arguments, "hasFlags"),
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
}
