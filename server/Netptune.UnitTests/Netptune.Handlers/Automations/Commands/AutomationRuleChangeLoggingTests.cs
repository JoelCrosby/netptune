using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Automation.Actions;
using Netptune.Automation.Rules;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Automations.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Automations.Commands;

public class AutomationRuleChangeLoggingTests
{
    private const int RuleId = 42;
    private const string UserId = "user-1";
    private const string ServiceUserId = "service-user";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IAutomationRepository Automations = Substitute.For<IAutomationRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IServiceAccountRepository ServiceAccounts = Substitute.For<IServiceAccountRepository>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly List<ActivityOptions<AutomationRuleActivityMeta>> Logged = [];

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public AutomationRuleChangeLoggingTests()
    {
        UnitOfWork.Automations.Returns(Automations);
        UnitOfWork.ServiceAccounts.Returns(ServiceAccounts);
        UnitOfWork.InvokeTransaction<int>();
        Identity.GetWorkspaceId().Returns(123);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceKey().Returns("workspace");
        PermissionCache.GetUserPermissions(UserId, "workspace").Returns(new UserPermissions
        {
            UserId = UserId,
            WorkspaceKey = "workspace",
            Role = WorkspaceRole.Owner,
            Permissions = [],
        });
        ServiceAccounts.GetAutomationPrincipal(Arg.Any<string>(), 123, Arg.Any<CancellationToken>())
            .Returns(call => new AutomationExecutionPrincipal
            {
                UserId = call.ArgAt<string>(0),
                IsEnabled = true,
                Permissions = NetptunePermissions.All,
            });
        Automations.CancelPendingRuleActions(RuleId, UserId, Arg.Any<CancellationToken>()).Returns(2);
        Activity
            .When(logger => logger.LogWith(Arg.Any<Action<ActivityOptions<AutomationRuleActivityMeta>>>()))
            .Do(call =>
            {
                var options = new ActivityOptions<AutomationRuleActivityMeta>();

                call.Arg<Action<ActivityOptions<AutomationRuleActivityMeta>>>().Invoke(options);
                Logged.Add(options);
            });

        var services = new ServiceCollection();
        services.AddNetptuneAutomationActions();
        ActionRegistry = services.BuildServiceProvider().GetRequiredService<IAutomationActionRegistry>();
    }

    [Fact]
    public async Task Create_ShouldLogCreation_WithTheRuleName()
    {
        var handler = new CreateAutomationRuleCommandHandler(UnitOfWork, Identity, ActionRegistry, PermissionCache, Activity);

        var result = await handler.Handle(new CreateAutomationRuleCommand(BuildRequest("New rule")), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        Logged.Should().ContainSingle();
        Logged[0].EntityType.Should().Be(EntityType.AutomationRule);
        Logged[0].Type.Should().Be(ActivityType.Create);
        Logged[0].Meta.Name.Should().Be("New rule");
    }

    [Fact]
    public async Task Update_ShouldKeepActionsAndSchedules_WhenOnlyTheNameChanges()
    {
        var rule = StoreRule(BuildRequest("Original"));
        var originalActions = rule.Actions.ToList();
        var handler = CreateUpdateHandler();

        var result = await handler.Handle(new UpdateAutomationRuleCommand(RuleId, BuildRequest("Renamed")), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        rule.Name.Should().Be("Renamed");
        rule.Actions.Should().BeEquivalentTo(originalActions);
        rule.Actions.Should().OnlyContain(action => !action.IsDeleted);
        await Automations.DidNotReceive().CancelPendingRuleActions(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Logged.Should().ContainSingle();
        Logged[0].Type.Should().Be(ActivityType.Modify);
        Logged[0].Meta.ChangedSections.Should().Equal(AutomationRuleChanges.Name);
        Logged[0].Meta.CancelledScheduledActions.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldReplaceActionsAndCancelSchedules_WhenActionsChange()
    {
        var rule = StoreRule(BuildRequest("Rule"));
        var request = BuildRequest("Rule") with
        {
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.AddComment,
                    Comment = "Picked up by automation",
                },
            ],
        };
        var handler = CreateUpdateHandler();

        var result = await handler.Handle(new UpdateAutomationRuleCommand(RuleId, request), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        rule.Actions.Where(action => !action.IsDeleted).Should().ContainSingle()
            .Which.Type.Should().Be(AutomationActionType.AddComment);
        await Automations.Received(1).CancelPendingRuleActions(RuleId, UserId, Arg.Any<CancellationToken>());
        Logged.Should().ContainSingle();
        Logged[0].Meta.ChangedSections.Should().Equal(AutomationRuleChanges.Actions);
        Logged[0].Meta.CancelledScheduledActions.Should().Be(2);
    }

    [Fact]
    public async Task Update_ShouldCancelSchedules_WhenTheTriggerChanges()
    {
        StoreRule(BuildRequest("Rule"));
        var request = BuildRequest("Rule") with
        {
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskUnassignedFor,
                DurationDays = 3,
            },
        };
        var handler = CreateUpdateHandler();

        await handler.Handle(new UpdateAutomationRuleCommand(RuleId, request), CancellationToken);

        await Automations.Received(1).CancelPendingRuleActions(RuleId, UserId, Arg.Any<CancellationToken>());
        Logged[0].Meta.ChangedSections.Should().Equal(AutomationRuleChanges.Trigger);
    }

    [Fact]
    public async Task Update_ShouldNotSaveOrLog_WhenNothingChanges()
    {
        StoreRule(BuildRequest("Rule"));
        var handler = CreateUpdateHandler();

        var result = await handler.Handle(new UpdateAutomationRuleCommand(RuleId, BuildRequest("Rule")), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
        Logged.Should().BeEmpty();
    }

    [Fact]
    public async Task Disable_ShouldCancelSchedulesAndLogDisable()
    {
        var rule = StoreRule(BuildRequest("Rule"));
        var handler = new SetAutomationRuleEnabledCommandHandler(UnitOfWork, Identity, Activity);

        var result = await handler.Handle(new SetAutomationRuleEnabledCommand(RuleId, false), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        rule.IsEnabled.Should().BeFalse();
        await Automations.Received(1).CancelPendingRuleActions(RuleId, UserId, Arg.Any<CancellationToken>());
        Logged.Should().ContainSingle();
        Logged[0].Type.Should().Be(ActivityType.Disable);
        Logged[0].Meta.CancelledScheduledActions.Should().Be(2);
    }

    [Fact]
    public async Task Enable_ShouldLogEnableWithoutCancelling()
    {
        var rule = StoreRule(BuildRequest("Rule") with { IsEnabled = false });
        var handler = new SetAutomationRuleEnabledCommandHandler(UnitOfWork, Identity, Activity);

        await handler.Handle(new SetAutomationRuleEnabledCommand(RuleId, true), CancellationToken);

        rule.IsEnabled.Should().BeTrue();
        await Automations.DidNotReceive().CancelPendingRuleActions(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Logged.Should().ContainSingle().Which.Type.Should().Be(ActivityType.Enable);
    }

    [Fact]
    public async Task SetEnabled_ShouldNotLog_WhenTheStateIsUnchanged()
    {
        StoreRule(BuildRequest("Rule"));
        var handler = new SetAutomationRuleEnabledCommandHandler(UnitOfWork, Identity, Activity);

        await handler.Handle(new SetAutomationRuleEnabledCommand(RuleId, true), CancellationToken);

        Logged.Should().BeEmpty();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldCancelSchedulesAndLogDeletion_WithTheRuleName()
    {
        var rule = StoreRule(BuildRequest("Doomed rule"));
        var handler = new DeleteAutomationRuleCommandHandler(UnitOfWork, Identity, Activity);

        var result = await handler.Handle(new DeleteAutomationRuleCommand(RuleId), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        rule.IsDeleted.Should().BeTrue();
        await Automations.Received(1).CancelPendingRuleActions(RuleId, UserId, Arg.Any<CancellationToken>());
        Logged.Should().ContainSingle();
        Logged[0].Type.Should().Be(ActivityType.Delete);
        Logged[0].Meta.Name.Should().Be("Doomed rule");
    }

    [Fact]
    public async Task Clone_ShouldLogCreation_WithTheSourceRule()
    {
        StoreRule(BuildRequest("Source"));
        var handler = new CloneAutomationRuleCommandHandler(UnitOfWork, Identity, ActionRegistry, Activity);

        var result = await handler.Handle(new CloneAutomationRuleCommand(RuleId, new AutomationCloneRequest()), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        Logged.Should().ContainSingle();
        Logged[0].Type.Should().Be(ActivityType.Create);
        Logged[0].Meta.ClonedFromRuleId.Should().Be(RuleId);
        Logged[0].Meta.Name.Should().Be("Source (copy)");
    }

    private UpdateAutomationRuleCommandHandler CreateUpdateHandler()
    {
        return new UpdateAutomationRuleCommandHandler(UnitOfWork, Identity, ActionRegistry, PermissionCache, Activity);
    }

    private static AutomationRuleRequest BuildRequest(string name)
    {
        return new AutomationRuleRequest
        {
            Name = name,
            IsEnabled = true,
            ExecutionUserId = ServiceUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskCreated,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Recipients = [AutomationNotificationRecipient.Assignees],
                },
            ],
        };
    }

    private AutomationRule StoreRule(AutomationRuleRequest request)
    {
        var rule = new AutomationRule
        {
            Id = RuleId,
            WorkspaceId = 123,
            Name = request.Name,
            IsEnabled = request.IsEnabled,
            ExecutionUserId = request.ExecutionUserId,
            TriggerType = request.Trigger.Type,
            TriggerConfig = AutomationMapping.ToTriggerConfig(request.Trigger),
            Actions = request.Actions.Select((action, index) => new AutomationAction
            {
                Id = index + 1,
                Type = action.Type,
                SortOrder = index,
                Config = AutomationMapping.ToActionConfig(action, ActionRegistry),
            }).ToList(),
        };

        Automations.GetRuleInWorkspace(RuleId, 123, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(rule);

        return rule;
    }
}
