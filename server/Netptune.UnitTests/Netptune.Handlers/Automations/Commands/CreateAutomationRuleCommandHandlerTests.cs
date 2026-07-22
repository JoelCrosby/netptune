using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
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

        Handler = new CreateAutomationRuleCommandHandler(UnitOfWork, Identity);
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
            ],
        };

        var result = await Handler.Handle(new CreateAutomationRuleCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        savedRule.Should().NotBeNull();
        savedRule!.WorkspaceId.Should().Be(123);
        savedRule.Name.Should().Be("Done notification");
        savedRule.Actions.Should().HaveCount(3);
        var commentAction = savedRule.Actions.Single(action => action.Type == AutomationActionType.AddComment);
        commentAction.Config!.RootElement.GetProperty("comment").GetString().Should().Be("Review requested by automation");
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
