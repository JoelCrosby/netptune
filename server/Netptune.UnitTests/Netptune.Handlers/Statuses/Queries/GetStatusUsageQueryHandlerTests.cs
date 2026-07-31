using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Usage;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;
using Netptune.Handlers.Statuses.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Statuses.Queries;

public class GetStatusUsageQueryHandlerTests
{
    private readonly GetStatusUsageQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IAutomationActionRegistry ActionRegistry = Substitute.For<IAutomationActionRegistry>();

    public GetStatusUsageQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, ActionRegistry);

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
    }

    [Fact]
    public async Task GetStatusUsage_ShouldReturnNull_WhenStatusNotInWorkspace()
    {
        UnitOfWork.Statuses.GetInWorkspace(4, 1, true, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusUsage_ShouldReturnCountsAndReferences_WhenStatusInUse()
    {
        var usage = new StatusUsage
        {
            TaskCount = 3,
            Projects = [new UsageReference { Id = 7, Name = "Apollo" }],
            BoardGroups = [new UsageReference { Id = 9, Name = "In Progress", Context = "Delivery" }],
        };

        GivenStatus(new Status { Id = 4, WorkspaceId = 1, Name = "In Progress" });
        GivenUsage(usage);
        GivenRules([]);

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.UsageCount.Should().Be(3);
        result.Kind.Should().Be(UsageSubjectKind.Status);
        result.References.Should().HaveCount(2);
        result.References[0].Kind.Should().Be(UsageReferenceKind.Project);
        result.References[1].Items[0].Context.Should().Be("Delivery");
        result.CanDelete.Should().BeFalse();
        result.BlockedReason.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatusUsage_ShouldAllowDeletion_WhenNothingUsesIt()
    {
        GivenStatus(new Status { Id = 4, WorkspaceId = 1, Name = "In Progress" });
        GivenUsage(new StatusUsage());
        GivenRules([]);

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result!.CanDelete.Should().BeTrue();
        result.BlockedReason.Should().BeNull();
        result.References.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusUsage_ShouldBlockDeletion_WhenStatusIsSystem()
    {
        GivenStatus(new Status { Id = 4, WorkspaceId = 1, Name = "New", IsSystem = true });
        GivenUsage(new StatusUsage());
        GivenRules([]);

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result!.CanDelete.Should().BeFalse();
        result.BlockedReason.Should().Be("System statuses cannot be deleted.");
    }

    [Fact]
    public async Task GetStatusUsage_ShouldReferenceAutomationRule_WhenActionSetsStatus()
    {
        var rule = new AutomationRule
        {
            Id = 11,
            WorkspaceId = 1,
            Name = "Move to in progress",
            TriggerType = AutomationTriggerType.TaskChanged,
            Actions = [new AutomationAction { Id = 21, Type = AutomationActionType.UpdateTask }],
        };

        GivenStatus(new Status { Id = 4, WorkspaceId = 1, Name = "In Progress" });
        GivenUsage(new StatusUsage());
        GivenRules([rule]);
        GivenActionView(AutomationActionType.UpdateTask, new AutomationActionViewModel
        {
            Id = 21,
            Type = AutomationActionType.UpdateTask,
            StatusId = 4,
        });

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result!.References.Should().ContainSingle();
        result.References[0].Kind.Should().Be(UsageReferenceKind.AutomationRule);
        result.References[0].Items[0].Name.Should().Be("Move to in progress");
    }

    [Fact]
    public async Task GetStatusUsage_ShouldReferenceAutomationRule_WhenTriggerConditionMatchesStatus()
    {
        var triggerConfig = JsonSerializer.SerializeToDocument(new
        {
            conditionGroup = new
            {
                conditions = new[]
                {
                    new { field = TaskChangeField.Status, @operator = AutomationConditionOperator.Equals, value = "4" },
                },
            },
        });

        var rule = new AutomationRule
        {
            Id = 12,
            WorkspaceId = 1,
            Name = "Notify on in progress",
            TriggerType = AutomationTriggerType.TaskChanged,
            TriggerConfig = triggerConfig,
        };

        GivenStatus(new Status { Id = 4, WorkspaceId = 1, Name = "In Progress" });
        GivenUsage(new StatusUsage());
        GivenRules([rule]);

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result!.References.Should().ContainSingle();
        result.References[0].Items[0].Id.Should().Be(12);
    }

    [Fact]
    public async Task GetStatusUsage_ShouldIgnoreAutomationRule_WhenItReferencesAnotherStatus()
    {
        var rule = new AutomationRule
        {
            Id = 13,
            WorkspaceId = 1,
            Name = "Move to done",
            TriggerType = AutomationTriggerType.TaskChanged,
            Actions = [new AutomationAction { Id = 23, Type = AutomationActionType.UpdateTask }],
        };

        GivenStatus(new Status { Id = 4, WorkspaceId = 1, Name = "In Progress" });
        GivenUsage(new StatusUsage());
        GivenRules([rule]);
        GivenActionView(AutomationActionType.UpdateTask, new AutomationActionViewModel
        {
            Id = 23,
            Type = AutomationActionType.UpdateTask,
            StatusId = 5,
        });

        var result = await Handler.Handle(new GetStatusUsageQuery(4), TestContext.Current.CancellationToken);

        result!.References.Should().BeEmpty();
    }

    private void GivenStatus(Status status)
    {
        UnitOfWork.Statuses
            .GetInWorkspace(status.Id, 1, true, TestContext.Current.CancellationToken)
            .Returns(status);
    }

    private void GivenUsage(StatusUsage usage)
    {
        UnitOfWork.Statuses
            .GetUsage(4, 1, TestContext.Current.CancellationToken)
            .Returns(usage);
    }

    private void GivenRules(List<AutomationRule> rules)
    {
        UnitOfWork.Automations
            .GetRulesInWorkspace(1, Arg.Any<bool>(), TestContext.Current.CancellationToken)
            .Returns(rules);
    }

    private void GivenActionView(AutomationActionType type, AutomationActionViewModel view)
    {
        var action = Substitute.For<IAutomationAction>();

        action.ToViewModel(Arg.Any<AutomationAction>()).Returns(view);
        ActionRegistry.Find(type).Returns(action);
    }
}
