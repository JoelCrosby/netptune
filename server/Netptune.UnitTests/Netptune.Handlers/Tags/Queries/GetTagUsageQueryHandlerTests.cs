using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tags.Queries;

public class GetTagUsageQueryHandlerTests
{
    private readonly GetTagUsageQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IAutomationActionRegistry ActionRegistry = Substitute.For<IAutomationActionRegistry>();

    public GetTagUsageQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, ActionRegistry);

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
    }

    [Fact]
    public async Task GetTagUsage_ShouldReturnNull_WhenTagNotFound()
    {
        UnitOfWork.Tags.GetAsync(3, true, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetTagUsageQuery(3), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTagUsage_ShouldReturnNull_WhenTagBelongsToAnotherWorkspace()
    {
        GivenTag(new Tag { Id = 3, WorkspaceId = 2, Name = "urgent" });

        var result = await Handler.Handle(new GetTagUsageQuery(3), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTagUsage_ShouldReturnTaskCount_WhenTagInWorkspace()
    {
        GivenTag(new Tag { Id = 3, WorkspaceId = 1, Name = "urgent" });
        GivenTaskCount(12);
        GivenRules([]);

        var result = await Handler.Handle(new GetTagUsageQuery(3), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.UsageCount.Should().Be(12);
        result.Name.Should().Be("urgent");
        result.Kind.Should().Be(UsageSubjectKind.Tag);
        result.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task GetTagUsage_ShouldReferenceAutomationRule_WhenActionAddsTag()
    {
        var rule = new AutomationRule
        {
            Id = 14,
            WorkspaceId = 1,
            Name = "Tag urgent work",
            TriggerType = AutomationTriggerType.TaskChanged,
            Actions = [new AutomationAction { Id = 24, Type = AutomationActionType.UpdateTask }],
        };

        GivenTag(new Tag { Id = 3, WorkspaceId = 1, Name = "urgent" });
        GivenTaskCount(0);
        GivenRules([rule]);
        GivenActionView(AutomationActionType.UpdateTask, new AutomationActionViewModel
        {
            Id = 24,
            Type = AutomationActionType.UpdateTask,
            AddTags = ["Urgent"],
        });

        var result = await Handler.Handle(new GetTagUsageQuery(3), TestContext.Current.CancellationToken);

        result!.References.Should().ContainSingle();
        result.References[0].Kind.Should().Be(UsageReferenceKind.AutomationRule);
        result.References[0].Items[0].Name.Should().Be("Tag urgent work");
    }

    private void GivenTag(Tag tag)
    {
        UnitOfWork.Tags
            .GetAsync(tag.Id, true, TestContext.Current.CancellationToken)
            .Returns(tag);
    }

    private void GivenTaskCount(int count)
    {
        UnitOfWork.Tags
            .GetTaskCount(3, TestContext.Current.CancellationToken)
            .Returns(count);
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
