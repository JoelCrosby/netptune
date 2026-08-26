using AutoFixture;
using AutoFixture.Dsl;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Relations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Services.ProjectTasks;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class CreateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly ITaskRelationLinker RelationLinker = Substitute.For<ITaskRelationLinker>();

    public CreateTaskCommandHandlerTests()
    {
        Fixture.Register(() => new DateOnly(2026, 7, 1));
        UnitOfWork.InvokeTransaction();

        // A request with no links plans to nothing; tests that pass links override both calls.
        RelationLinker.Plan(Arg.Any<TaskRelationPlanRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TaskRelationPlan());
        RelationLinker.Apply(Arg.Any<TaskRelationPlan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        // The resolver only reads through the unit of work, so the real one keeps these tests asserting
        // what actually lands on the task. The linker writes and publishes, so that one is substituted.
        Handler = new(
            UnitOfWork,
            Identity,
            Activity,
            EventPublisher,
            EventRecords,
            RelationLinker,
            new TaskReferenceResolver(UnitOfWork),
            new TaskStatusResolver(UnitOfWork));
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenStartDateIsAfterDueDate()
    {
        var request = new AddProjectTaskRequest
        {
            Name = "Invalid schedule",
            ProjectId = 1,
            StartDate = new DateOnly(2026, 7, 20),
            DueDate = new DateOnly(2026, 7, 19),
        };

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ProjectTaskSchedule.InvalidDateRangeMessage);
    }

    private IPostprocessComposer<AddProjectTaskRequest> BuildRequest()
    {
        return Fixture.Build<AddProjectTaskRequest>()
            .Without(p => p.SprintId)
            .Without(p => p.AssigneeId)
            .Without(p => p.AssigneeIds)
            .Without(p => p.Tags)
            .Without(p => p.Relations)
            .With(p => p.ProjectId, 1)
            .With(p => p.BoardGroupId, 1);
    }

    private void SetupStatusDependencies()
    {
        var status = AutoFixtures.TaskStatus with
        {
            Id = 5,
            WorkspaceId = 1,
            Category = StatusCategory.Todo,
        };

        UnitOfWork.Statuses.GetInWorkspace(
            Arg.Any<int>(),
            1,
            Arg.Any<bool>(),
            TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Statuses.GetTaskStatusByKey(1, "new", TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Statuses.GetFirstTaskStatus(1, TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Statuses.GetFirstTaskStatusByCategory(1, Arg.Any<StatusCategory>(), TestContext.Current.CancellationToken)
            .Returns(status);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = BuildRequest().Create();
        var viewModel = new TaskViewModel
        {
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };
        var createdTask = AutoFixtures.ProjectTask;
        var currentUser = AutoFixtures.AppUser;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(currentUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(createdTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                BoardId = 1,
                MaxSortOrder = 7,
            });

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.SortOrder.Should().Be(request.SortOrder);

        await EventPublisher.Received(1).Dispatch(Arg.Is<TaskCreatedMessage>(message =>
            message.TaskId == createdTask.Id &&
            message.WorkspaceId == 1 &&
            message.ActorUserId == currentUser.Id));
    }

    [Fact]
    public async Task Create_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = BuildRequest().Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                BoardId = 1,
                MaxSortOrder = 7,
            });

        await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(3).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = BuildRequest().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .ReturnsNull();

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = BuildRequest().Create();

        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeRefIdNull()
    {
        var request = BuildRequest().Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                BoardId = 1,
                MaxSortOrder = 7,
            });

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldAssignEveryRequestedAssignee()
    {
        var members = new List<AppUser>
        {
            AutoFixtures.AppUserFixture.With(p => p.Id, "user-a").Create(),
            AutoFixtures.AppUserFixture.With(p => p.Id, "user-b").Create(),
        };
        var request = BuildRequest()
            .With(p => p.AssigneeIds, ["user-a", "user-b"])
            .Create();

        SetupCreateDependencies(request);
        UnitOfWork.Users.IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            1,
            TestContext.Current.CancellationToken)
            .Returns(members);

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Tasks.Received(1).AddAsync(
            Arg.Is<ProjectTask>(task => task.ProjectTaskAppUsers
                .Select(assignee => assignee.UserId)
                .OrderBy(id => id)
                .SequenceEqual(new[] { "user-a", "user-b" })),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldAssignTheCreator_WhenNoAssigneeRequested()
    {
        var request = BuildRequest().Create();
        var setup = SetupCreateDependencies(request);

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Tasks.Received(1).AddAsync(
            Arg.Is<ProjectTask>(task => task.ProjectTaskAppUsers.Single().UserId == setup.CurrentUser.Id),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenAssigneeIsNotAWorkspaceMember()
    {
        var request = BuildRequest()
            .With(p => p.AssigneeIds, ["outsider"])
            .Create();

        SetupCreateDependencies(request);
        UnitOfWork.Users.IsUserInWorkspaceRange(
            Arg.Any<IEnumerable<string>>(),
            1,
            TestContext.Current.CancellationToken)
            .Returns([]);

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("outsider");

        await UnitOfWork.Tasks.DidNotReceive().AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldAttachRequestedTags()
    {
        var tag = AutoFixtures.Tag with { Id = 12, Name = "backend" };
        var request = BuildRequest()
            .With(p => p.Tags, ["backend"])
            .Create();

        SetupCreateDependencies(request);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(
            1,
            Arg.Any<IEnumerable<string>>(),
            true,
            TestContext.Current.CancellationToken)
            .Returns([tag]);

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Tasks.Received(1).AddAsync(
            Arg.Is<ProjectTask>(task => task.ProjectTaskTags.Single().TagId == 12),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenTagIsNotInTheWorkspace()
    {
        var request = BuildRequest()
            .With(p => p.Tags, ["missing-tag"])
            .Create();

        SetupCreateDependencies(request);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(
            1,
            Arg.Any<IEnumerable<string>>(),
            true,
            TestContext.Current.CancellationToken)
            .Returns([]);

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("missing-tag");

        await UnitOfWork.Tasks.DidNotReceive().AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldApplyAndPublishThePlannedRelations()
    {
        var request = BuildRequest()
            .With(p => p.Relations, [BlockedByRelation("NP-2")])
            .Create();
        var setup = SetupCreateDependencies(request);
        var planned = PlanFor(request);

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await RelationLinker.Received(1).Apply(planned, setup.CreatedTask.Id, TestContext.Current.CancellationToken);
        await RelationLinker.Received(1).Publish(
            Arg.Is<IReadOnlyCollection<LinkedTaskRelation>>(links => links.Count == 1),
            setup.CurrentUser.Id);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_AndCreateNothing_WhenTheRelationPlanIsRejected()
    {
        var request = BuildRequest()
            .With(p => p.Relations, [BlockedByRelation("NP-404")])
            .Create();

        SetupCreateDependencies(request);
        RelationLinker.Plan(Arg.Any<TaskRelationPlanRequest>(), TestContext.Current.CancellationToken)
            .Returns(TaskRelationPlan.Failed("Task with key NP-404 not found"));

        var result = await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("NP-404");

        await UnitOfWork.Tasks.DidNotReceive().AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken);
        await RelationLinker.DidNotReceive().Apply(
            Arg.Any<TaskRelationPlan>(),
            Arg.Any<int>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldPlanRelationsInTheCallersWorkspace()
    {
        var request = BuildRequest()
            .With(p => p.Relations, [BlockedByRelation("NP-2")])
            .Create();

        SetupCreateDependencies(request);

        await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        await RelationLinker.Received(1).Plan(
            Arg.Is<TaskRelationPlanRequest>(planRequest =>
                planRequest.WorkspaceId == 1 &&
                planRequest.WorkspaceKey == "key" &&
                planRequest.Links.Count == 1),
            TestContext.Current.CancellationToken);
    }

    private static AddTaskRelationRequest BlockedByRelation(string systemId)
    {
        return new AddTaskRelationRequest
        {
            RelatedSystemId = systemId,
            RelationTypeId = 3,
            TaskIsSource = false,
        };
    }

    // The plan the substituted linker hands back for a request, so tests can assert it is the one applied.
    private TaskRelationPlan PlanFor(AddProjectTaskRequest request)
    {
        var relationType = new RelationType
        {
            Id = 3,
            WorkspaceId = 1,
            Name = "Blocks",
            InverseName = "Is Blocked By",
            Key = "blocks",
            Category = RelationCategory.Dependency,
        };
        var relatedTask = new TaskViewModel { Id = 42, SystemId = "NP-2", Name = "Blocker" };
        var links = (request.Relations ?? [])
            .ConvertAll(link => new PlannedTaskRelation(relationType, relatedTask, link.TaskIsSource));
        var plan = new TaskRelationPlan { WorkspaceId = 1, Relations = links };

        RelationLinker.Plan(Arg.Any<TaskRelationPlanRequest>(), TestContext.Current.CancellationToken).Returns(plan);
        RelationLinker.Apply(plan, Arg.Any<int>(), TestContext.Current.CancellationToken)
            .Returns(links.ConvertAll(_ => new LinkedTaskRelation(
                new ProjectTaskRelation { WorkspaceId = 1, RelationTypeId = relationType.Id },
                relationType.Category)));

        return plan;
    }

    private CreateSetup SetupCreateDependencies(AddProjectTaskRequest request)
    {
        var currentUser = AutoFixtures.AppUser;
        var createdTask = AutoFixtures.ProjectTask;
        var viewModel = new TaskViewModel
        {
            Id = createdTask.Id,
            Name = request.Name,
            Description = request.Description,
            SystemId = "NP-1",
        };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(currentUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(request.ProjectId!.Value, "Project", 1, 5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(createdTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken)
            .Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                BoardId = 1,
                MaxSortOrder = 7,
            });

        return new CreateSetup(currentUser, createdTask);
    }

    private sealed record CreateSetup(AppUser CurrentUser, ProjectTask CreatedTask);

    [Fact]
    public async Task Create_ShouldLogActivity_WhenInputValid()
    {
        var request = BuildRequest().Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        SetupStatusDependencies();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Projects.GetTaskCreationProject(request.ProjectId!.Value, 1, TestContext.Current.CancellationToken)
            .Returns(new TaskCreationProject(
                request.ProjectId!.Value,
                "Project",
                1,
                5));
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Projects.ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.BoardGroupId.Value,
                Name = "Group",
                BoardId = 1,
                MaxSortOrder = 7,
            });

        await Handler.Handle(new CreateTaskCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}
