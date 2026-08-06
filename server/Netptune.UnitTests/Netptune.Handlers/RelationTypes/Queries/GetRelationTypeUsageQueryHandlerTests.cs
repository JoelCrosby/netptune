using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;
using Netptune.Handlers.RelationTypes.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.RelationTypes.Queries;

public class GetRelationTypeUsageQueryHandlerTests
{
    private readonly GetRelationTypeUsageQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IAutomationActionRegistry ActionRegistry = Substitute.For<IAutomationActionRegistry>();

    public GetRelationTypeUsageQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, ActionRegistry);

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
    }

    [Fact]
    public async Task GetRelationTypeUsage_ShouldReturnNull_WhenRelationTypeNotInWorkspace()
    {
        UnitOfWork.RelationTypes.GetInWorkspace(6, 1, true, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetRelationTypeUsageQuery(6), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRelationTypeUsage_ShouldBlockDeletion_WhenRelationsExist()
    {
        GivenRelationType(new RelationType { Id = 6, WorkspaceId = 1, Name = "Blocks" });
        GivenRelationCount(4);
        GivenRules([]);

        var result = await Handler.Handle(new GetRelationTypeUsageQuery(6), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.UsageCount.Should().Be(4);
        result.Kind.Should().Be(UsageSubjectKind.RelationType);
        result.CanDelete.Should().BeFalse();
        result.BlockedReason.Should().Be("Relation type is in use and cannot be deleted.");
    }

    [Fact]
    public async Task GetRelationTypeUsage_ShouldBlockDeletion_WhenRelationTypeIsSystem()
    {
        GivenRelationType(new RelationType { Id = 6, WorkspaceId = 1, Name = "Blocks", IsSystem = true });
        GivenRelationCount(0);
        GivenRules([]);

        var result = await Handler.Handle(new GetRelationTypeUsageQuery(6), TestContext.Current.CancellationToken);

        result!.CanDelete.Should().BeFalse();
        result.BlockedReason.Should().Be("Built-in relation types cannot be deleted.");
    }

    [Fact]
    public async Task GetRelationTypeUsage_ShouldReferenceAutomationRule_WhenActionLinksTasks()
    {
        var rule = new AutomationRule
        {
            Id = 15,
            WorkspaceId = 1,
            Name = "Link duplicates",
            TriggerType = AutomationTriggerType.TaskChanged,
            Actions = [new AutomationAction { Id = 25, Type = AutomationActionType.ManageTaskRelation }],
        };

        GivenRelationType(new RelationType { Id = 6, WorkspaceId = 1, Name = "Blocks" });
        GivenRelationCount(0);
        GivenRules([rule]);
        GivenActionView(AutomationActionType.ManageTaskRelation, new AutomationActionViewModel
        {
            Id = 25,
            Type = AutomationActionType.ManageTaskRelation,
            RelationTypeId = 6,
        });

        var result = await Handler.Handle(new GetRelationTypeUsageQuery(6), TestContext.Current.CancellationToken);

        result!.References.Should().ContainSingle();
        result.References[0].Items[0].Name.Should().Be("Link duplicates");
        result.CanDelete.Should().BeTrue();
    }

    private void GivenRelationType(RelationType relationType)
    {
        UnitOfWork.RelationTypes
            .GetInWorkspace(relationType.Id, 1, true, TestContext.Current.CancellationToken)
            .Returns(relationType);
    }

    private void GivenRelationCount(int count)
    {
        UnitOfWork.RelationTypes
            .GetRelationCount(6, TestContext.Current.CancellationToken)
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
