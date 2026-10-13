using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Relations.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Relations.Queries;

public class FindTaskRelationViolationQueryHandlerTests
{
    private const int WorkspaceId = 1;
    private const int RelationTypeId = 5;

    private readonly FindTaskRelationViolationQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public FindTaskRelationViolationQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);

        Identity.GetWorkspaceKey().Returns("workspace");

        UnitOfWork.Workspaces
            .GetIdBySlug("workspace", Arg.Any<CancellationToken>())
            .Returns(WorkspaceId);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTheLinkIsAllowed()
    {
        GivenRelationType(RelationCategory.Dependency);

        var result = await Handle(20, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCheckASymmetricLinkInItsCanonicalOrder()
    {
        GivenRelationType(RelationCategory.Related);

        UnitOfWork.ProjectTaskRelations
            .Exists(RelationTypeId, 10, 20, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle(20, 10);

        result.Should().Be("These tasks are already linked by this relation.");
    }

    [Fact]
    public async Task Handle_ShouldReportASecondParent_ForAHierarchyType()
    {
        GivenRelationType(RelationCategory.Hierarchy);

        UnitOfWork.ProjectTaskRelations
            .HasExistingSource(RelationTypeId, 20, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle(10, 20);

        result.Should().Contain("can only have one");
    }

    [Fact]
    public async Task Handle_ShouldReportACycle_ForADependencyType()
    {
        GivenRelationType(RelationCategory.Dependency);

        UnitOfWork.ProjectTaskRelations
            .WouldCreateCycle(RelationTypeId, 10, 20, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle(10, 20);

        result.Should().Contain("circular");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTheRelationTypeIsNotInTheWorkspace()
    {
        UnitOfWork.RelationTypes
            .GetInWorkspace(RelationTypeId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await Handle(10, 20);

        result.Should().BeNull();
        await UnitOfWork.ProjectTaskRelations
            .DidNotReceive()
            .Exists(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private async Task<string?> Handle(int taskId, int otherTaskId)
    {
        var query = new FindTaskRelationViolationQuery(RelationTypeId, taskId, otherTaskId);

        return await Handler.Handle(query, TestContext.Current.CancellationToken);
    }

    private void GivenRelationType(RelationCategory category)
    {
        UnitOfWork.RelationTypes
            .GetInWorkspace(RelationTypeId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new RelationType
            {
                Id = RelationTypeId,
                WorkspaceId = WorkspaceId,
                Name = "Blocks",
                Key = "blocks",
                Category = category,
            });
    }
}
