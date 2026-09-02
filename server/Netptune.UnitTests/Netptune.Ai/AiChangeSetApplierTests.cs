using System.Text.Json;

using FluentAssertions;

using Mediator;

using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Projects;
using Netptune.Handlers.Projects.Commands;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Services.Ai;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiChangeSetApplierTests
{
    private const string UserId = "user-1";
    private const int WorkspaceId = 7;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly IAiChangeSetRepository ChangeSets = Substitute.For<IAiChangeSetRepository>();
    private readonly IWorkspaceUserRepository WorkspaceUsers = Substitute.For<IWorkspaceUserRepository>();
    private readonly IWorkspaceRepository Workspaces = Substitute.For<IWorkspaceRepository>();
    private readonly IAiConversationRepository Conversations = Substitute.For<IAiConversationRepository>();
    private readonly ITaskRepository Tasks = Substitute.For<ITaskRepository>();
    private readonly AiCancellationRegistry Cancellations = new();

    public AiChangeSetApplierTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(Task.FromResult(WorkspaceId));
        Identity.GetWorkspaceKey().Returns("netptune");

        UnitOfWork.AiChangeSets.Returns(ChangeSets);
        UnitOfWork.WorkspaceUsers.Returns(WorkspaceUsers);
        UnitOfWork.Workspaces.Returns(Workspaces);
        UnitOfWork.AiConversations.Returns(Conversations);
        UnitOfWork.Tasks.Returns(Tasks);

        Tasks
            .GetTaskViewModels(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var taskIds = call.Arg<IEnumerable<int>>();
                var models = taskIds
                    .Select(taskId => new TaskViewModel { Id = taskId, SystemId = $"netp-{taskId}" })
                    .ToList();

                return Task.FromResult(models);
            });

        Workspaces
            .GetAsync(WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Workspace?>(new Workspace
            {
                Id = WorkspaceId,
                Name = "Netptune",
                Slug = "netptune",
                AssistantEnabled = true,
            }));
    }

    [Fact]
    public async Task Apply_ShouldThrow_WhenTheWorkspaceHasTheAssistantTurnedOff()
    {
        var changeSet = CreateChangeSet();

        GivenChangeSet(changeSet, []);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        Workspaces
            .GetAsync(WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Workspace?>(new Workspace
            {
                Id = WorkspaceId,
                Name = "Netptune",
                Slug = "netptune",
                AssistantEnabled = false,
            }));

        var applier = CreateApplier();
        var apply = () => applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        await apply.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Apply_ShouldReturnNull_WhenTheChangeSetIsNotOwned()
    {
        ChangeSets
            .GetOwned(Arg.Any<Guid>(), UserId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AiChangeSet?>(null));

        var applier = CreateApplier();
        var result = await applier.Apply(Guid.NewGuid(), new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Apply_ShouldThrow_WhenTheUserLacksThePermissionForAChange()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Read);

        var applier = CreateApplier();
        var apply = () => applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        await apply.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Apply_ShouldThrow_WhenTheChangeSetIsNotPending()
    {
        var changeSet = CreateChangeSet();

        changeSet.Status = AiChangeSetStatus.Applied;

        GivenChangeSet(changeSet, []);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var applier = CreateApplier();
        var apply = () => applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        await apply.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Apply_ShouldSkipChangesThatWereNotSelected()
    {
        var changeSet = CreateChangeSet();
        var selected = CreateChange(changeSet.Id, "propose_create_task", id: 1);
        var unselected = CreateChange(changeSet.Id, "propose_create_task", id: 2);

        GivenChangeSet(changeSet, [selected, unselected]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var applier = CreateApplier();
        var request = new ApplyAiChangeSetRequest { ChangeIds = [selected.Id] };
        var result = await applier.Apply(changeSet.Id, request, null, TestContext.Current.CancellationToken);

        result!.Results.Should().ContainSingle(item => item.ChangeId == selected.Id);
        unselected.ApplyStatus.Should().Be(AiChangeApplyStatus.Skipped);
    }

    [Fact]
    public async Task Apply_ShouldNotApplyChangesThatFailedValidation()
    {
        var changeSet = CreateChangeSet();
        var invalid = CreateChange(changeSet.Id, "propose_create_task");

        invalid = invalid with { ValidationStatus = AiChangeValidationStatus.Invalid };

        GivenChangeSet(changeSet, [invalid]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var applier = CreateApplier();
        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        result!.Results.Should().BeEmpty();
        invalid.ApplyStatus.Should().Be(AiChangeApplyStatus.Skipped);
    }

    [Fact]
    public async Task Apply_ShouldFailTheChange_WhenNoHandlerIsRegisteredForItsTool()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_unhandled_thing");

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var tools = new AiToolRegistry([
            new StubWriteTool("propose_unhandled_thing", NetptunePermissions.Tasks.Create),
        ]);

        var applier = new AiChangeSetApplier(
            UnitOfWork,
            Identity,
            tools,
            new AiExecutionContext(),
            new AiCancellationRegistry(),
            NullLogger<AiChangeSetApplier>.Instance,
            []);

        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        result!.Results.Should().ContainSingle();
        result.Results[0].Status.Should().Be(AiChangeApplyStatus.Failed);
        change.ApplyStatus.Should().Be(AiChangeApplyStatus.Failed);
    }

    private AiChangeSetApplier CreateApplier()
    {
        var tools = new AiToolRegistry([
            new StubWriteTool("propose_create_task", NetptunePermissions.Tasks.Create),
            new StubWriteTool("propose_create_project", NetptunePermissions.Projects.Create),
        ]);

        return new AiChangeSetApplier(
            UnitOfWork,
            Identity,
            tools,
            new AiExecutionContext(),
            Cancellations,
            NullLogger<AiChangeSetApplier>.Instance,
            [new CreateTaskChangeHandler(Mediator), new CreateProjectChangeHandler(Mediator)]);
    }

    private void GivenChangeSet(AiChangeSet changeSet, List<AiProposedChange> changes)
    {
        ChangeSets
            .GetOwned(changeSet.Id, UserId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AiChangeSet?>(changeSet));

        ChangeSets
            .GetChanges(changeSet.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(changes));
    }

    private void GivenPermissions(params string[] permissions)
    {
        var userPermissions = new UserPermissions
        {
            UserId = UserId,
            WorkspaceKey = "netptune",
            Role = WorkspaceRole.Owner,
            Permissions = [.. permissions],
        };

        WorkspaceUsers
            .GetUserPermissions(UserId, "netptune", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UserPermissions?>(userPermissions));
    }

    private static AiChangeSet CreateChangeSet()
    {
        return new AiChangeSet
        {
            Id = Guid.NewGuid(),
            WorkspaceId = WorkspaceId,
            ConversationId = Guid.NewGuid(),
            MessageId = 1,
            UserId = UserId,
            Status = AiChangeSetStatus.Pending,
            CorrelationId = Guid.NewGuid(),
        };
    }

    [Fact]
    public async Task Apply_ShouldRunAChangeBeforeAnythingThatReferencesIt()
    {
        var changeSet = CreateChangeSet();
        var dependent = CreateChange(
            changeSet.Id,
            "propose_create_task",
            id: 1,
            refKey: "ref:dependent",
            payload: """{"name":"Child","projectId":1,"sprintRef":"ref:sprint"}""");

        var prerequisite = CreateChange(changeSet.Id, "propose_create_task", id: 2, refKey: "ref:sprint");

        GivenChangeSet(changeSet, [dependent, prerequisite]);
        GivenPermissions(NetptunePermissions.Tasks.Create);
        GivenCreatedTask(7);

        var applier = CreateApplier();
        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        result!.Results.Select(item => item.ChangeId).Should().Equal(
            [prerequisite.Id, dependent.Id],
            "a proposal cannot apply against an id its prerequisite has not created yet");
    }

    [Fact]
    public async Task Apply_ShouldGiveATaskTheProjectItsPrerequisiteCreated()
    {
        var changeSet = CreateChangeSet();
        var project = CreateChange(
            changeSet.Id,
            "propose_create_project",
            id: 1,
            refKey: "ref:project",
            payload: """{"name":"Apollo"}""");

        var task = CreateChange(
            changeSet.Id,
            "propose_create_task",
            id: 2,
            refKey: "ref:task",
            payload: """{"name":"Draft the brief","projectRef":"ref:project"}""");

        GivenChangeSet(changeSet, [task, project]);
        GivenPermissions(NetptunePermissions.Tasks.Create, NetptunePermissions.Projects.Create);
        GivenCreatedProject(55);
        GivenCreatedTask(7);

        var applier = CreateApplier();

        await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        await Mediator
            .Received(1)
            .Send(
                Arg.Is<CreateTaskCommand>(command => command.Request.ProjectId == 55),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_ShouldSkipADependent_WhenItsPrerequisiteNeverRan()
    {
        var changeSet = CreateChangeSet();
        var dependent = CreateChange(
            changeSet.Id,
            "propose_create_task",
            id: 1,
            payload: """{"name":"Child","projectId":1,"sprintRef":"ref:missing"}""");

        GivenChangeSet(changeSet, [dependent]);
        GivenPermissions(NetptunePermissions.Tasks.Create);
        GivenCreatedTask(7);

        var applier = CreateApplier();
        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);
        var outcome = result!.Results.Single();

        outcome.Status.Should().Be(AiChangeApplyStatus.Skipped);
        outcome.Error.Should().Contain("ref:missing");
    }

    [Fact]
    public async Task Apply_ShouldTellTheConversationWhatWasCreated()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Create);
        GivenCreatedTask(91);
        GivenConversation(changeSet.ConversationId);

        var applier = CreateApplier();

        await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), null, TestContext.Current.CancellationToken);

        await Conversations
            .Received(1)
            .AddMessage(
                Arg.Is<AiMessage>(message => message.Content.RootElement.GetRawText().Contains("id 91")),
                Arg.Any<CancellationToken>());
    }

    private void GivenConversation(Guid conversationId)
    {
        Conversations
            .GetAsync(conversationId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AiConversation?>(new AiConversation
            {
                Id = conversationId,
                WorkspaceId = WorkspaceId,
                UserId = UserId,
                Title = "Chat",
                Model = "claude-opus-5",
            }));
    }

    [Fact]
    public async Task Apply_ShouldReportEveryChangeAsItLands()
    {
        var changeSet = CreateChangeSet();
        var first = CreateChange(changeSet.Id, "propose_create_task", id: 1, refKey: "ref:1");
        var second = CreateChange(changeSet.Id, "propose_create_task", id: 2, refKey: "ref:2");

        GivenChangeSet(changeSet, [first, second]);
        GivenPermissions(NetptunePermissions.Tasks.Create);
        GivenCreatedTask(7);

        var applier = CreateApplier();
        var reported = new List<AiApplyProgress>();

        await applier.Apply(
            changeSet.Id,
            new ApplyAiChangeSetRequest(),
            progress =>
            {
                reported.Add(progress);

                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        reported[0].Type.Should().Be(AiApplyProgressType.Started);
        reported[0].Total.Should().Be(2);

        var completions = reported
            .Where(progress => progress.Type == AiApplyProgressType.ChangeCompleted)
            .ToList();

        completions.Select(progress => progress.ChangeId).Should().Equal(
            [first.Id, second.Id],
            "the client counts a run by the changes it is told about");

        completions.Select(progress => progress.Completed).Should().Equal([1, 2]);
    }

    [Fact]
    public async Task Apply_ShouldFailWhatNeverRan_WhenTheRunIsStopped()
    {
        var changeSet = CreateChangeSet();
        var first = CreateChange(changeSet.Id, "propose_create_task", id: 1, refKey: "ref:1");
        var second = CreateChange(changeSet.Id, "propose_create_task", id: 2, refKey: "ref:2");

        GivenChangeSet(changeSet, [first, second]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Cancellations.Stop(changeSet.Id);

                return ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = 7 });
            });

        var applier = CreateApplier();
        var result = await applier.Apply(
            changeSet.Id,
            new ApplyAiChangeSetRequest(),
            null,
            TestContext.Current.CancellationToken);

        first.ApplyStatus.Should().Be(AiChangeApplyStatus.Applied);
        second.ApplyStatus.Should().Be(
            AiChangeApplyStatus.Failed,
            "a change the run never reached is not something the workspace has");

        second.ApplyError.Should().Contain("Stopped");
        result!.Status.Should().Be(AiChangeSetStatus.PartiallyApplied);
    }

    private void GivenCreatedTask(int taskId)
    {
        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = taskId }));
    }

    private void GivenCreatedProject(int projectId)
    {
        Mediator
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<ProjectViewModel>.Success(new ProjectViewModel { Id = projectId }));
    }

    [Fact]
    public async Task Undo_ShouldDeleteATaskTheChangeSetCreated()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        change.ApplyStatus = AiChangeApplyStatus.Applied;
        change.AppliedEntityId = 42;
        changeSet.Status = AiChangeSetStatus.Applied;
        changeSet.AppliedAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Delete);

        Mediator
            .Send(Arg.Any<DeleteTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);

        var applier = CreateApplier();
        var result = await applier.Undo(changeSet.Id, TestContext.Current.CancellationToken);

        result!.Results.Single().Status.Should().Be(AiChangeApplyStatus.Applied);
        change.UndoneAt.Should().NotBeNull("an undone change must not be undone twice");
        changeSet.UndoneAt.Should().NotBeNull();

        await Mediator.Received(1).Send(
            Arg.Is<DeleteTaskCommand>(command => command.Id == 42),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Undo_ShouldThrow_WhenTheUserCannotReverseTheChange()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        change.ApplyStatus = AiChangeApplyStatus.Applied;
        change.AppliedEntityId = 42;
        changeSet.Status = AiChangeSetStatus.Applied;
        changeSet.AppliedAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var applier = CreateApplier();
        var undo = async () => await applier.Undo(changeSet.Id, TestContext.Current.CancellationToken);

        await undo.Should().ThrowAsync<UnauthorizedAccessException>(
            "creating a task and deleting it again are different permissions");
    }

    [Fact]
    public async Task Undo_ShouldThrow_WhenTheChangeSetWasNeverApplied()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Delete);

        var applier = CreateApplier();
        var undo = async () => await applier.Undo(changeSet.Id, TestContext.Current.CancellationToken);

        await undo.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Undo_ShouldLeaveAChangeInPlace_WhenReversingItFails()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        change.ApplyStatus = AiChangeApplyStatus.Applied;
        change.AppliedEntityId = 42;
        changeSet.Status = AiChangeSetStatus.Applied;
        changeSet.AppliedAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Delete);

        Mediator
            .Send(Arg.Any<DeleteTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Failed("The task is locked."));

        var applier = CreateApplier();
        var result = await applier.Undo(changeSet.Id, TestContext.Current.CancellationToken);
        var outcome = result!.Results.Single();

        outcome.Status.Should().Be(AiChangeApplyStatus.Failed);
        outcome.Error.Should().Be("The task is locked.");
        change.UndoneAt.Should().BeNull("a failed undo has to stay available to retry");
        changeSet.UndoneAt.Should().BeNull();
    }

    [Fact]
    public async Task RetryFailed_ShouldRunOnlyTheChangesThatFailed()
    {
        var changeSet = CreateChangeSet();
        var applied = CreateChange(changeSet.Id, "propose_create_task", id: 1, refKey: "ref:done");
        var failed = CreateChange(changeSet.Id, "propose_create_task", id: 2, refKey: "ref:retry");

        applied.ApplyStatus = AiChangeApplyStatus.Applied;
        applied.AppliedEntityId = 11;
        failed.ApplyStatus = AiChangeApplyStatus.Failed;
        failed.ApplyError = "The project was locked.";
        changeSet.Status = AiChangeSetStatus.PartiallyApplied;
        changeSet.AppliedAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [applied, failed]);
        GivenPermissions(NetptunePermissions.Tasks.Create);
        GivenCreatedTask(12);

        var applier = CreateApplier();
        var result = await applier.RetryFailed(changeSet.Id, TestContext.Current.CancellationToken);

        result!.Results.Select(item => item.ChangeId).Should().Equal([failed.Id]);
        failed.ApplyStatus.Should().Be(AiChangeApplyStatus.Applied);
        failed.ApplyError.Should().BeNull();
        applied.AppliedEntityId.Should().Be(11, "a change that already landed must not run twice");
        changeSet.Status.Should().Be(AiChangeSetStatus.Applied);
    }

    [Fact]
    public async Task RetryFailed_ShouldResolveReferencesFromTheFirstAttempt()
    {
        var changeSet = CreateChangeSet();
        var prerequisite = CreateChange(changeSet.Id, "propose_create_task", id: 1, refKey: "ref:sprint");
        var dependent = CreateChange(
            changeSet.Id,
            "propose_create_task",
            id: 2,
            refKey: "ref:child",
            payload: """{"name":"Child","projectId":1,"sprintRef":"ref:sprint"}""");

        prerequisite.ApplyStatus = AiChangeApplyStatus.Applied;
        prerequisite.AppliedEntityId = 30;
        dependent.ApplyStatus = AiChangeApplyStatus.Failed;
        changeSet.Status = AiChangeSetStatus.PartiallyApplied;
        changeSet.AppliedAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [prerequisite, dependent]);
        GivenPermissions(NetptunePermissions.Tasks.Create);
        GivenCreatedTask(31);

        var applier = CreateApplier();
        var result = await applier.RetryFailed(changeSet.Id, TestContext.Current.CancellationToken);

        result!.Results.Single().Status.Should().Be(
            AiChangeApplyStatus.Applied,
            "the id its prerequisite created the first time is still good");
    }

    [Fact]
    public async Task RetryFailed_ShouldThrow_WhenNothingFailed()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        change.ApplyStatus = AiChangeApplyStatus.Applied;
        changeSet.Status = AiChangeSetStatus.Applied;
        changeSet.AppliedAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var applier = CreateApplier();
        var retry = async () => await applier.RetryFailed(changeSet.Id, TestContext.Current.CancellationToken);

        await retry.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RetryFailed_ShouldThrow_WhenTheChangeSetWasUndone()
    {
        var changeSet = CreateChangeSet();
        var change = CreateChange(changeSet.Id, "propose_create_task");

        change.ApplyStatus = AiChangeApplyStatus.Failed;
        changeSet.Status = AiChangeSetStatus.PartiallyApplied;
        changeSet.AppliedAt = DateTime.UtcNow;
        changeSet.UndoneAt = DateTime.UtcNow;

        GivenChangeSet(changeSet, [change]);
        GivenPermissions(NetptunePermissions.Tasks.Create);

        var applier = CreateApplier();
        var retry = async () => await applier.RetryFailed(changeSet.Id, TestContext.Current.CancellationToken);

        await retry.Should().ThrowAsync<InvalidOperationException>(
            "putting changes back after an undo would contradict the undo");
    }

    private static AiProposedChange CreateChange(
        Guid changeSetId,
        string toolName,
        long id = 1,
        string refKey = "ref:1",
        string payload = """{"name":"Test","projectId":1}""")
    {
        return new AiProposedChange
        {
            Id = id,
            ChangeSetId = changeSetId,
            Sequence = (int)id,
            ToolName = toolName,
            EntityType = "task",
            RefKey = refKey,
            Summary = "Create task",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };
    }

    private sealed class StubWriteTool : IAiTool
    {
        public StubWriteTool(string name, string permission)
        {
            Name = name;
            RequiredPermissions = new HashSet<string>(StringComparer.Ordinal) { permission };
        }

        public string Name { get; }

        public string Description => "stub";

        public AiToolKind Kind => AiToolKind.Write;

        public IReadOnlySet<string> RequiredPermissions { get; }

        public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

        public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
        {
            return Task.FromResult(AiToolExecution.Success("ok"));
        }
    }
}
