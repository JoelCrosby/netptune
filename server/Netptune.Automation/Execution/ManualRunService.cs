using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Matching;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class ManualRunService : IAutomationManualRunService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly RuleExecutor RuleExecutor;
    private readonly ILogger<ManualRunService> Logger;

    public ManualRunService(
        INetptuneUnitOfWork unitOfWork,
        RuleExecutor ruleExecutor,
        ILogger<ManualRunService> logger)
    {
        UnitOfWork = unitOfWork;
        RuleExecutor = ruleExecutor;
        Logger = logger;
    }

    public async Task<AutomationManualRunResult> Execute(
        AutomationManualRunRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.StartActivity("automation.execute_manual_run");

        activity?.SetTag("automation.rule_id", request.RuleId);
        activity?.SetTag("automation.tasks.requested", request.TaskIds.Count);

        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(
            request.RuleId,
            request.WorkspaceId,
            cancellationToken: cancellationToken);

        if (rule is null)
        {
            return AutomationManualRunResult.NotFound;
        }

        var runToken = Guid.NewGuid();
        var triggeredAt = DateTime.UtcNow;
        var executions = new List<PendingAutomationExecution>();
        var skippedCount = 0;

        foreach (var taskId in request.TaskIds.Distinct())
        {
            var task = await UnitOfWork.Tasks.GetAutomationTask(taskId, cancellationToken);
            var isEligible = task is not null
                && task.WorkspaceId == request.WorkspaceId
                && AutomationRuleConditions.Match(rule, task);

            if (!isEligible)
            {
                skippedCount++;

                continue;
            }

            executions.Add(CreateExecution(rule, task!, request, runToken, triggeredAt));
        }

        Logger.LogInformation(
            "Manual automation run for rule {RuleId} matched {ExecutedCount} of {RequestedCount} tasks",
            rule.Id,
            executions.Count,
            request.TaskIds.Count);

        await RuleExecutor.Execute(rule.TriggerType, executions, cancellationToken);

        return new AutomationManualRunResult
        {
            RuleFound = true,
            ExecutedCount = executions.Count,
            SkippedCount = skippedCount,
        };
    }

    private static PendingAutomationExecution CreateExecution(
        AutomationRule rule,
        ProjectTask task,
        AutomationManualRunRequest request,
        Guid runToken,
        DateTime triggeredAt)
    {
        return new PendingAutomationExecution
        {
            Rule = rule,
            Task = task,
            ExecutionUserId = rule.ExecutionUserId,
            InitiatingUserId = request.InitiatingUserId,
            IdempotencyKey = $"rule:{rule.Id}:task:{task.Id}:manual:{runToken}",
            TriggeredAt = triggeredAt,
            CorrelationId = runToken,
        };
    }
}
