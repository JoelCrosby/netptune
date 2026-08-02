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

    public AiChangeSetApplierTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(Task.FromResult(WorkspaceId));
        Identity.GetWorkspaceKey().Returns("netptune");

        UnitOfWork.AiChangeSets.Returns(ChangeSets);
        UnitOfWork.WorkspaceUsers.Returns(WorkspaceUsers);
        UnitOfWork.Workspaces.Returns(Workspaces);
        UnitOfWork.AiConversations.Returns(Conversations);

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
        var apply = () => applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

        await apply.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Apply_ShouldReturnNull_WhenTheChangeSetIsNotOwned()
    {
        ChangeSets
            .GetOwned(Arg.Any<Guid>(), UserId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AiChangeSet?>(null));

        var applier = CreateApplier();
        var result = await applier.Apply(Guid.NewGuid(), new ApplyAiChangeSetRequest(), CancellationToken.None);

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
        var apply = () => applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

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
        var apply = () => applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

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
        var result = await applier.Apply(changeSet.Id, request, CancellationToken.None);

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
        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

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
            Mediator,
            tools,
            new AiExecutionContext(),
            NullLogger<AiChangeSetApplier>.Instance,
            []);

        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

        result!.Results.Should().ContainSingle();
        result.Results[0].Status.Should().Be(AiChangeApplyStatus.Failed);
        change.ApplyStatus.Should().Be(AiChangeApplyStatus.Failed);
    }

    private AiChangeSetApplier CreateApplier()
    {
        var tools = new AiToolRegistry([new StubWriteTool("propose_create_task", NetptunePermissions.Tasks.Create)]);

        return new AiChangeSetApplier(
            UnitOfWork,
            Identity,
            Mediator,
            tools,
            new AiExecutionContext(),
            NullLogger<AiChangeSetApplier>.Instance,
            [new CreateTaskChangeHandler(Mediator)]);
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
        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

        result!.Results.Select(item => item.ChangeId).Should().Equal(
            [prerequisite.Id, dependent.Id],
            "a proposal cannot apply against an id its prerequisite has not created yet");
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
        var result = await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);
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

        await applier.Apply(changeSet.Id, new ApplyAiChangeSetRequest(), CancellationToken.None);

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

    private void GivenCreatedTask(int taskId)
    {
        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = taskId }));
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
        var result = await applier.Undo(changeSet.Id, CancellationToken.None);

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
        var undo = async () => await applier.Undo(changeSet.Id, CancellationToken.None);

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
        var undo = async () => await applier.Undo(changeSet.Id, CancellationToken.None);

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
        var result = await applier.Undo(changeSet.Id, CancellationToken.None);
        var outcome = result!.Results.Single();

        outcome.Status.Should().Be(AiChangeApplyStatus.Failed);
        outcome.Error.Should().Be("The task is locked.");
        change.UndoneAt.Should().BeNull("a failed undo has to stay available to retry");
        changeSet.UndoneAt.Should().BeNull();
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
