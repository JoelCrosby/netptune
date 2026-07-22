using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Automation.Actions;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Automations.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Automations.Commands;

public class CreateAutomationRuleCommandHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IAutomationRepository Automations = Substitute.For<IAutomationRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly CreateAutomationRuleCommandHandler Handler;

    public CreateAutomationRuleCommandHandlerTests()
    {
        UnitOfWork.Automations.Returns(Automations);
        Identity.GetWorkspaceId().Returns(123);
        Identity.GetCurrentUserId().Returns("user-1");

        var services = new ServiceCollection();
        services.AddNetptuneAutomationActions();
        var actionRegistry = services
            .BuildServiceProvider()
            .GetRequiredService<IAutomationActionRegistry>();

        Handler = new CreateAutomationRuleCommandHandler(UnitOfWork, Identity, actionRegistry);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenStatusChangedTriggerMissingStatus()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Done notification",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskStatusChanged,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("status");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTaskChangedTriggerMissingFields()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Task changed notification",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("field");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenFlagActionMissingFlagName()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Unassigned flag",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskUnassignedFor,
                DurationDays = 3,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.FlagTask,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("flagName");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUpdateTaskActionMissingFields()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Unassigned task update",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskUnassignedFor,
                DurationDays = 3,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.UpdateTask,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("status or priority");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAddCommentActionMissingComment()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Comment on unassigned task",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskUnassignedFor,
                DurationDays = 3,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.AddComment,
                },
            ],
        };

        var command = new CreateAutomationRuleCommand(request);
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("comment");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenConditionFieldIsNotWatched()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Conditional notification",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Name],
                Conditions =
                [
                    new AutomationFieldCondition(
                        TaskChangeField.Priority,
                        AutomationConditionOperator.Equals,
                        "High"),
                ],
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("included in fields");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenConditionValueIsMissing()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Conditional notification",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Name],
                Conditions =
                [
                    new AutomationFieldCondition(
                        TaskChangeField.Name,
                        AutomationConditionOperator.Contains,
                        null),
                ],
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("requires a value");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenDueDateTriggerHasInvalidDuration()
    {
        var request = new AutomationRuleRequest
        {
            Name = "Due-date notification",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskDueDateApproaching,
                DurationDays = -1,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("durationDays");
        await Automations.DidNotReceive().AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCreateRule_WhenRequestValid()
    {
        AutomationRule? savedRule = null;
        Automations
            .AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                savedRule = call.Arg<AutomationRule>();
                savedRule.Id = 42;
                return savedRule;
            });

        var request = new AutomationRuleRequest
        {
            Name = " Done notification ",
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Status],
                Conditions =
                [
                    new AutomationFieldCondition(
                        TaskChangeField.Status,
                        AutomationConditionOperator.Equals,
                        "12"),
                ],
                StatusId = 12,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Message = "Task completed",
                },
                new AutomationActionRequest
                {
                    Type = AutomationActionType.FlagTask,
                    FlagName = "Review",
                },
                new AutomationActionRequest
                {
                    Type = AutomationActionType.AddComment,
                    Comment = "Review requested by automation",
                },
                new AutomationActionRequest
                {
                    Type = AutomationActionType.DeleteTask,
                    DelayAmount = 2,
                    DelayUnit = AutomationDelayUnit.Hours,
                },
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        savedRule.Should().NotBeNull();
        savedRule!.WorkspaceId.Should().Be(123);
        savedRule.Name.Should().Be("Done notification");
        savedRule.Actions.Should().HaveCount(4);

        var condition = savedRule.TriggerConfig!.RootElement.GetProperty("conditions")[0];
        condition.GetProperty("field").GetInt32().Should().Be((int)TaskChangeField.Status);
        condition.GetProperty("operator").GetInt32().Should().Be((int)AutomationConditionOperator.Equals);
        condition.GetProperty("value").GetString().Should().Be("12");

        var commentAction = savedRule.Actions.Single(action => action.Type == AutomationActionType.AddComment);
        commentAction.Config!.RootElement.GetProperty("comment").GetString().Should().Be("Review requested by automation");

        var deleteAction = savedRule.Actions.Single(action => action.Type == AutomationActionType.DeleteTask);
        deleteAction.Config!.RootElement.GetProperty("delayAmount").GetInt32().Should().Be(2);
        deleteAction.Config.RootElement.GetProperty("delayUnit").GetInt32().Should().Be((int)AutomationDelayUnit.Hours);
        result.Payload!.Actions.Single(action => action.Type == AutomationActionType.DeleteTask)
            .DelayAmount.Should().Be(2);
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
