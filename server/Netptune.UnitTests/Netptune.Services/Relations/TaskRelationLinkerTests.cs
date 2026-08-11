using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Relations;
using Netptune.Core.Models.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Relations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services.Relations;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Relations;

public class TaskRelationLinkerTests
{
    private const int WorkspaceId = 1;
    private const string WorkspaceKey = "key";
    private const int BlocksId = 3;
    private const int NewTaskId = 7;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly TaskRelationLinker Linker;

    public TaskRelationLinkerTests()
    {
        Linker = new TaskRelationLinker(UnitOfWork, Activity, EventPublisher);
    }

    [Fact]
    public async Task Plan_ShouldReturnAnEmptyPlan_AndQueryNothing_WhenNoLinksRequested()
    {
        var plan = await Plan([]);

        plan.IsValid.Should().BeTrue();
        plan.Relations.Should().BeEmpty();

        await UnitOfWork.RelationTypes.DidNotReceive().GetAllInWorkspace(
            Arg.Any<int>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await UnitOfWork.Tasks.DidNotReceive().GetTaskViewModels(
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Plan_ShouldResolveEveryLink_WhenInputValid()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"));

        var plan = await Plan([Forward("NP-2"), Inverse("NP-3")]);

        plan.IsValid.Should().BeTrue();
        plan.WorkspaceId.Should().Be(WorkspaceId);
        plan.Relations.Should().HaveCount(2);
        plan.Relations.Should().ContainSingle(relation => relation.Task.Id == 42 && relation.TaskIsSource);
        plan.Relations.Should().ContainSingle(relation => relation.Task.Id == 43 && !relation.TaskIsSource);
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenRelationTypeNotFound()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"));

        var plan = await Plan([new AddTaskRelationRequest { RelatedSystemId = "NP-2", RelationTypeId = 99 }]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("99");
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenRelatedTaskNotFound()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks();

        var plan = await Plan([Forward("NP-404")]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("NP-404");
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenALinkHasNoTaskKey()
    {
        SetupWorkspace(RelationCategory.Dependency);

        var plan = await Plan([new AddTaskRelationRequest { RelatedSystemId = "  ", RelationTypeId = BlocksId }]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenTheSameRelationIsRequestedTwice()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"));

        var plan = await Plan([Forward("NP-2"), Forward("NP-2")]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("linked more than once");
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenOneTaskIsLinkedBothWays()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"));

        var plan = await Plan([Forward("NP-2"), Inverse("NP-2")]);

        // Linking one task both ways is the same task and relation type twice, so it is refused as a
        // duplicate from the request alone, without walking the graph.
        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("linked more than once");

        await UnitOfWork.ProjectTaskRelations.DidNotReceive().GetReachableTaskIds(
            Arg.Any<int>(),
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenRelatedTaskAlreadyHasASingleSourceLink()
    {
        SetupWorkspace(RelationCategory.Hierarchy);
        SetupRelatedTasks(Task(42, "NP-2"));
        UnitOfWork.ProjectTaskRelations.GetTargetsWithExistingSource(
            BlocksId,
            Arg.Is<IReadOnlyCollection<int>>(targetIds => targetIds.Contains(42)),
            Arg.Any<CancellationToken>())
            .Returns([42]);

        var plan = await Plan([Forward("NP-2")]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("only have one");
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenTwoLinksClaimTheSameSingleSourceEnd()
    {
        SetupWorkspace(RelationCategory.Hierarchy);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"));

        var plan = await Plan([Inverse("NP-2"), Inverse("NP-3")]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("only have one");
    }

    [Fact]
    public async Task Plan_ShouldFail_WhenLinkingBothEndsWouldCloseACycle()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"));
        UnitOfWork.ProjectTaskRelations.GetReachableTaskIds(
            BlocksId,
            Arg.Is<IReadOnlyCollection<int>>(fromTaskIds => fromTaskIds.Contains(42)),
            Arg.Any<CancellationToken>())
            .Returns([43]);

        var plan = await Plan([Forward("NP-2"), Inverse("NP-3")]);

        plan.IsValid.Should().BeFalse();
        plan.Error.Should().Contain("circular");
    }

    [Fact]
    public async Task Plan_ShouldNotWalkTheGraph_WhenOnlyOneEndIsLinked()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"));

        var plan = await Plan([Forward("NP-2"), Forward("NP-3")]);

        plan.IsValid.Should().BeTrue();

        await UnitOfWork.ProjectTaskRelations.DidNotReceive().GetReachableTaskIds(
            Arg.Any<int>(),
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Plan_ShouldQueryOncePerConcern_HoweverManyLinks()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"), Task(44, "NP-4"));

        var plan = await Plan([Forward("NP-2"), Forward("NP-3"), Inverse("NP-4")]);

        plan.IsValid.Should().BeTrue();

        await UnitOfWork.RelationTypes.Received(1).GetAllInWorkspace(
            WorkspaceId,
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await UnitOfWork.Tasks.Received(1).GetTaskViewModels(
            Arg.Any<IReadOnlyCollection<string>>(),
            WorkspaceKey,
            Arg.Any<CancellationToken>());
        await UnitOfWork.ProjectTaskRelations.Received(1).GetReachableTaskIds(
            BlocksId,
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<CancellationToken>());
        await UnitOfWork.ProjectTaskRelations.DidNotReceive().WouldCreateCycle(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_ShouldInsertEveryLink_InOneSave()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"));
        SetupNewTask();

        var plan = await Plan([Forward("NP-2"), Inverse("NP-3")]);
        var links = await Linker.Apply(plan, NewTaskId, TestContext.Current.CancellationToken);

        links.Should().HaveCount(2);

        await UnitOfWork.ProjectTaskRelations.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<ProjectTaskRelation>>(relations => relations.Count() == 2),
            Arg.Any<CancellationToken>());
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_ShouldOrientEachLink_ByItsDirection()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"), Task(43, "NP-3"));
        SetupNewTask();

        var plan = await Plan([Forward("NP-2"), Inverse("NP-3")]);
        var links = await Linker.Apply(plan, NewTaskId, TestContext.Current.CancellationToken);
        var forward = links.Select(link => link.Relation).Single(relation => relation.TargetTaskId == 42);
        var inverse = links.Select(link => link.Relation).Single(relation => relation.SourceTaskId == 43);

        forward.SourceTaskId.Should().Be(NewTaskId);
        inverse.TargetTaskId.Should().Be(NewTaskId);
    }

    [Fact]
    public async Task Apply_ShouldRecordActivity_AgainstBothEndsOfEveryLink()
    {
        SetupWorkspace(RelationCategory.Dependency);
        SetupRelatedTasks(Task(42, "NP-2"));
        SetupNewTask();

        var plan = await Plan([Forward("NP-2")]);

        await Linker.Apply(plan, NewTaskId, TestContext.Current.CancellationToken);

        Activity.Received(2).LogWith(Arg.Any<Action<ActivityOptions<TaskRelationActivityMeta>>>());
    }

    [Fact]
    public async Task Apply_ShouldDoNothing_WhenThePlanHasNoLinks()
    {
        var plan = await Plan([]);
        var links = await Linker.Apply(plan, NewTaskId, TestContext.Current.CancellationToken);

        links.Should().BeEmpty();

        await UnitOfWork.ProjectTaskRelations.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<ProjectTaskRelation>>(),
            Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_ShouldDispatchOneMessagePerLink()
    {
        var links = new List<LinkedTaskRelation>
        {
            new(Relation(42, NewTaskId), RelationCategory.Dependency),
            new(Relation(NewTaskId, 43), RelationCategory.Dependency),
        };

        await Linker.Publish(links, "user-a");

        await EventPublisher.Received(2).Dispatch(Arg.Any<TaskRelationChangedMessage>());
        await EventPublisher.Received(1).Dispatch(Arg.Is<TaskRelationChangedMessage>(message =>
            message.SourceTaskId == 42 &&
            message.TargetTaskId == NewTaskId &&
            message.ActorUserId == "user-a" &&
            message.Change == TaskRelationChange.Added));
    }

    private Task<TaskRelationPlan> Plan(List<AddTaskRelationRequest> links)
    {
        return Linker.Plan(
            new TaskRelationPlanRequest
            {
                WorkspaceId = WorkspaceId,
                WorkspaceKey = WorkspaceKey,
                Links = links,
            },
            TestContext.Current.CancellationToken);
    }

    private void SetupWorkspace(RelationCategory category)
    {
        var relationType = new RelationType
        {
            Id = BlocksId,
            WorkspaceId = WorkspaceId,
            Name = "Blocks",
            InverseName = "Is Blocked By",
            Key = "blocks",
            Category = category,
        };

        UnitOfWork.RelationTypes.GetAllInWorkspace(
            WorkspaceId,
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns([relationType]);
        UnitOfWork.ProjectTaskRelations.GetTargetsWithExistingSource(
            Arg.Any<int>(),
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<CancellationToken>())
            .Returns([]);
        UnitOfWork.ProjectTaskRelations.GetReachableTaskIds(
            Arg.Any<int>(),
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private void SetupRelatedTasks(params TaskViewModel[] tasks)
    {
        UnitOfWork.Tasks.GetTaskViewModels(
            Arg.Any<IReadOnlyCollection<string>>(),
            WorkspaceKey,
            Arg.Any<CancellationToken>())
            .Returns(tasks.ToList());
    }

    private void SetupNewTask()
    {
        UnitOfWork.Tasks.GetTaskViewModel(NewTaskId, Arg.Any<CancellationToken>())
            .Returns(Task(NewTaskId, "NP-1"));
    }

    private static TaskViewModel Task(int id, string systemId)
    {
        return new TaskViewModel { Id = id, SystemId = systemId, Name = systemId };
    }

    private static ProjectTaskRelation Relation(int sourceTaskId, int targetTaskId)
    {
        return new ProjectTaskRelation
        {
            WorkspaceId = WorkspaceId,
            RelationTypeId = BlocksId,
            SourceTaskId = sourceTaskId,
            TargetTaskId = targetTaskId,
        };
    }

    private static AddTaskRelationRequest Forward(string systemId)
    {
        return new AddTaskRelationRequest { RelatedSystemId = systemId, RelationTypeId = BlocksId };
    }

    private static AddTaskRelationRequest Inverse(string systemId)
    {
        return new AddTaskRelationRequest { RelatedSystemId = systemId, RelationTypeId = BlocksId, TaskIsSource = false };
    }
}
