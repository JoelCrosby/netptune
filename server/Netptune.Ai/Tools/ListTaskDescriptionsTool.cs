using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListTaskDescriptionsTool : IAiTool
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 100;

    private readonly IMediator Mediator;

    public ListTaskDescriptionsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_task_descriptions";

    public string Description =>
        "List every task in the workspace, or in one project, in systemId order with the shape of its description: "
        + "taskId, systemId, name, project, characters and format. "
        + "Use it to triage descriptions across many tasks without reading each one. "
        + "With includeDescriptions it also returns each description in full as markdown. "
        + "Ask for the next page until hasMore is false; omittedForSize counts tasks on this page that did not fit, "
        + "so ask again with a smaller pageSize. "
        + AiTextFormatJson.Description;

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "projectId": { "type": "integer", "description": "Only list tasks in this project." },
          "includeDescriptions": { "type": "boolean", "description": "Also return each full description. Defaults to false." },
          "page": { "type": "integer", "description": "Which page of results to return, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many tasks to return per page, up to 100. Defaults to 50." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var page = Math.Max(AiToolSchema.GetInt(arguments, "page") ?? 1, 1);
        var projectId = AiToolSchema.GetInt(arguments, "projectId");
        var pageRequest = new PageRequest { Page = page, PageSize = pageSize };

        var result = await Mediator.Send(new GetTaskTextsQuery(projectId, pageRequest), cancellationToken);

        var hasTasks = result.IsSuccess && result.Payload is not null;

        if (!hasTasks)
        {
            return AiToolExecution.Failed(result.Message ?? "Tasks could not be read.");
        }

        var tasks = result.Payload!;
        var includesDescriptions = AiToolSchema.GetBool(arguments, "includeDescriptions") ?? false;
        var rows = tasks.Items.Select(task =>
        {
            var format = AiTextFormatReader.Read(task.Description);

            return new
            {
                taskId = task.Id,
                systemId = task.SystemId,
                name = task.Name,
                project = task.ProjectName,
                characters = format.Characters,
                format = AiTextFormatJson.From(format),
                description = includesDescriptions ? task.Description : null,
            };
        });

        var hasMore = page * pageSize < tasks.TotalCount;
        var fitted = AiResultBudget.Fit(rows);
        var listing = new JsonObject
        {
            ["totalCount"] = tasks.TotalCount,
            ["page"] = page,
            ["hasMore"] = hasMore,
            ["tasks"] = fitted.Rows,
        };

        if (fitted.Omitted > 0)
        {
            listing["omittedForSize"] = fitted.Omitted;
        }

        return AiToolExecution.Success(listing.ToJsonString());
    }
}
