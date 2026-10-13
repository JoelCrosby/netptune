using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Automations.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListAutomationRunsTool : IAiTool
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly IMediator Mediator;

    public ListAutomationRunsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_automation_runs";

    public string Description =>
        "Recent runs of one automation rule, with what triggered each run, whether it succeeded, and what it did. "
        + "Use this to explain why a rule did or did not fire on a particular task. "
        + "Ask for the next page until hasMore is false.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Automations.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "ruleId": { "type": "integer", "description": "The automation rule id, from list_automations." },
          "page": { "type": "integer", "description": "Which page of runs to return, newest first, starting at 1." },
          "pageSize": { "type": "integer", "description": "How many runs to return per page, up to 100. Defaults to 20." }
        }
        """,
        "ruleId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var ruleId = AiToolSchema.GetInt(arguments, "ruleId");

        if (!ruleId.HasValue)
        {
            return AiToolExecution.Failed("A ruleId is required.");
        }

        var requestedPageSize = AiToolSchema.GetInt(arguments, "pageSize") ?? DefaultPageSize;
        var pageSize = Math.Clamp(requestedPageSize, 1, MaximumPageSize);
        var page = Math.Max(AiToolSchema.GetInt(arguments, "page") ?? 1, 1);
        var filter = new AutomationRunFilter
        {
            Page = page,
            PageSize = pageSize,
        };

        var result = await Mediator.Send(new GetAutomationRunsQuery(ruleId.Value, filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(
                result.Message ?? $"Automation rule {ruleId} was not found in this workspace.");
        }

        var runs = result.Payload.Items
            .Select(run => new
            {
                id = run.Id,
                status = run.Status.ToString(),
                trigger = run.TriggerType.ToString(),
                message = run.Message,
                entityId = run.EntityId,
                entityType = run.EntityType?.ToString(),
                createdAt = run.CreatedAt,
                actionResults = run.ActionResults.Select(action => new
                {
                    type = action.ActionType.ToString(),
                    status = action.Status.ToString(),
                    message = action.Message,
                }),
            });

        var hasMore = page * pageSize < result.Payload.TotalCount;
        var listing = new
        {
            totalCount = result.Payload.TotalCount,
            page,
            hasMore,
            runs,
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(listing));
    }
}
