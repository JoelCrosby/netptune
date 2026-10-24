using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Execution;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Automation.Execution;

public class AutomationLimitGuardTests
{
    private const int RuleId = 7;
    private const string ServiceUserId = "service-user";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IAutomationRepository Automations = Substitute.For<IAutomationRepository>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly AutomationLimitGuard Guard;

    public AutomationLimitGuardTests()
    {
        UnitOfWork.Automations.Returns(Automations);
        UnitOfWork.InvokeTransaction();
        Automations.GetRunStats(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([new AutomationRunStats(RuleId, 20, 20)]);
        Automations.CancelPendingRuleActions(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [RuleId] = 3 });

        var options = Options.Create(new LimitsOptions { WorkspaceRunQuota = 0 });

        Guard = new AutomationLimitGuard(UnitOfWork, EventRecords, options, NullLogger<AutomationLimitGuard>.Instance);
    }

    [Fact]
    public async Task Filter_ShouldSaveTheAutoDisableEvent_WhenEveryExecutionIsTripped()
    {
        var allowed = await Guard.Filter(AutomationTriggerType.TaskCreated, [BuildExecution()], TestContext.Current.CancellationToken);

        allowed.Should().BeEmpty();

        Received.InOrder(() =>
        {
            Automations.AutoDisableRules(
                Arg.Is<IReadOnlyDictionary<int, string>>(reasons => reasons.ContainsKey(RuleId)),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>());
            Automations.CancelPendingRuleActions(
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(RuleId)),
                Arg.Any<CancellationToken>());
            EventRecords.AppendRange(
                Arg.Is<IReadOnlyList<IEventWriteRequest>>(events => IsAutoDisabledEvent(events)),
                Arg.Any<CancellationToken>());
            UnitOfWork.CompleteAsync(Arg.Any<CancellationToken>());
        });

        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }

    private static bool IsAutoDisabledEvent(IReadOnlyList<IEventWriteRequest> events)
    {
        var disabledEvent = events.Single();
        var payload = (AutomationRuleActivityMeta)disabledEvent.Payload;
        var isAutoDisabledKey = disabledEvent.EventKey == EventKeys.AutomationRuleAutoDisabled;
        var isActorTheServiceAccount = disabledEvent.ActorUserId == ServiceUserId;
        var hasCancelledCount = payload.CancelledScheduledActions == 3;

        return isAutoDisabledKey && isActorTheServiceAccount && hasCancelledCount;
    }

    private static PendingAutomationExecution BuildExecution()
    {
        return new PendingAutomationExecution
        {
            Rule = new AutomationRule
            {
                Id = RuleId,
                WorkspaceId = 1,
                Name = "Flaky rule",
                IsEnabled = true,
                ExecutionUserId = ServiceUserId,
            },
            Task = new ProjectTask { Id = 11, WorkspaceId = 1 },
            IdempotencyKey = "key",
            TriggeredAt = DateTime.UtcNow,
        };
    }
}
