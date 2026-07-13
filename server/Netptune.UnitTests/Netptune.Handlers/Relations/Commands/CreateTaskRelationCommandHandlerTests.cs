using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Models.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Relations;
using Netptune.Handlers.Relations.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Relations.Commands;

public class CreateTaskRelationCommandHandlerTests
{
    private const int WorkspaceId = 1;
    private const int SourceTaskId = 10;
    private const int TargetTaskId = 20;

    private readonly CreateTaskRelationCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateTaskRelationCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenInputValid()
    {
        Arrange(RelationCategory.Dependency);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.ProjectTaskRelations
            .Received(1)
            .AddAsync(
                Arg.Is<ProjectTaskRelation>(relation =>
                    relation.SourceTaskId == SourceTaskId &&
                    relation.TargetTaskId == TargetTaskId &&
                    relation.WorkspaceId == WorkspaceId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldFail_WhenTaskRelatedToItself()
    {
        Arrange(RelationCategory.Dependency);

        // Both system ids resolve to the same task.
        StubTask("TARGET-2", SourceTaskId);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("cannot be related to itself");

        await UnitOfWork.ProjectTaskRelations
            .DidNotReceive()
            .AddAsync(Arg.Any<ProjectTaskRelation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldFail_WhenRelationWouldCreateCycle()
    {
        Arrange(RelationCategory.Dependency);

        UnitOfWork.ProjectTaskRelations
            .WouldCreateCycle(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("circular");

        await UnitOfWork.ProjectTaskRelations
            .DidNotReceive()
            .AddAsync(Arg.Any<ProjectTaskRelation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldFail_WhenHierarchyTargetAlreadyHasAParent()
    {
        Arrange(RelationCategory.Hierarchy);

        UnitOfWork.ProjectTaskRelations
            .HasExistingSource(Arg.Any<int>(), TargetTaskId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("can only have one");

        await UnitOfWork.ProjectTaskRelations
            .DidNotReceive()
            .AddAsync(Arg.Any<ProjectTaskRelation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldNotCheckSingleSource_ForNonHierarchyCategories()
    {
        Arrange(RelationCategory.Dependency);

        // A task can be blocked by many things — only hierarchy is exclusive.
        UnitOfWork.ProjectTaskRelations
            .HasExistingSource(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldNotCheckCycles_ForDuplicateCategory()
    {
        Arrange(RelationCategory.Duplicate);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.ProjectTaskRelations
            .DidNotReceive()
            .WouldCreateCycle(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldFail_WhenRelationAlreadyExists()
    {
        Arrange(RelationCategory.Dependency);

        UnitOfWork.ProjectTaskRelations
            .Exists(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("already linked");
    }

    [Fact]
    public async Task Create_ShouldStoreSymmetricRelationsInCanonicalOrder()
    {
        Arrange(RelationCategory.Related);

        // The higher task id is passed as the source, so the handler has to flip the pair. Without
        // that, "A relates to B" and "B relates to A" would be two distinct rows.
        StubTask("SOURCE-1", TargetTaskId);
        StubTask("TARGET-2", SourceTaskId);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.ProjectTaskRelations
            .Received(1)
            .AddAsync(
                Arg.Is<ProjectTaskRelation>(relation =>
                    relation.SourceTaskId == SourceTaskId &&
                    relation.TargetTaskId == TargetTaskId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldNotReorder_ForDirectionalCategories()
    {
        Arrange(RelationCategory.Dependency);

        StubTask("SOURCE-1", TargetTaskId);
        StubTask("TARGET-2", SourceTaskId);

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsSuccess.Should().BeTrue();

        // "B blocks A" must stay "B blocks A" — direction carries meaning here.
        await UnitOfWork.ProjectTaskRelations
            .Received(1)
            .AddAsync(
                Arg.Is<ProjectTaskRelation>(relation =>
                    relation.SourceTaskId == TargetTaskId &&
                    relation.TargetTaskId == SourceTaskId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenTaskIsInAnotherWorkspace()
    {
        Arrange(RelationCategory.Dependency);

        // Tasks resolve through the workspace key, so a task outside the workspace simply does not
        // resolve — this is what keeps relations from spanning workspaces.
        UnitOfWork.Tasks
            .GetTaskViewModel("TARGET-2", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenRelationTypeIsInAnotherWorkspace()
    {
        Arrange(RelationCategory.Dependency);

        UnitOfWork.RelationTypes
            .GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await Handle("SOURCE-1", "TARGET-2");

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldLogActivity_AgainstBothTasks()
    {
        Arrange(RelationCategory.Dependency);

        await Handle("SOURCE-1", "TARGET-2");

        // One entry per task, so the relation reads correctly in either task's feed.
        Activity.Received(2).LogWith(Arg.Any<Action<ActivityOptions<TaskRelationActivityMeta>>>());
    }

    private ValueTask<ClientResponse<TaskRelationViewModel>> Handle(string sourceSystemId, string targetSystemId)
    {
        var request = new CreateTaskRelationRequest
        {
            SourceSystemId = sourceSystemId,
            TargetSystemId = targetSystemId,
            RelationTypeId = 5,
        };

        return Handler.Handle(new CreateTaskRelationCommand(request), TestContext.Current.CancellationToken);
    }

    private void Arrange(RelationCategory category)
    {
        Identity.GetWorkspaceKey().Returns("workspace");
        Identity.GetCurrentUserId().Returns("user-id");

        UnitOfWork.Workspaces
            .GetIdBySlug(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(WorkspaceId);

        UnitOfWork.RelationTypes
            .GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new RelationType
            {
                Id = 5,
                WorkspaceId = WorkspaceId,
                Name = "Blocks",
                InverseName = "Is Blocked By",
                Key = "blocks",
                Category = category,
            });

        StubTask("SOURCE-1", SourceTaskId);
        StubTask("TARGET-2", TargetTaskId);

        UnitOfWork.ProjectTaskRelations
            .Exists(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
        UnitOfWork.ProjectTaskRelations
            .HasExistingSource(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
        UnitOfWork.ProjectTaskRelations
            .WouldCreateCycle(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        UnitOfWork.InvokeTransaction<ClientResponse<TaskRelationViewModel>>();
    }

    private void StubTask(string systemId, int taskId)
    {
        UnitOfWork.Tasks
            .GetTaskViewModel(systemId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TaskViewModel
            {
                Id = taskId,
                SystemId = systemId,
                Name = $"Task {taskId}",
                StatusName = "New",
                StatusCategory = StatusCategory.Todo,
            });
    }

}
