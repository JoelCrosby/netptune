using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
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

    public AiChangeSetApplierTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetWorkspaceId().Returns(Task.FromResult(WorkspaceId));
        Identity.GetWorkspaceKey().Returns("netptune");

        UnitOfWork.AiChangeSets.Returns(ChangeSets);
        UnitOfWork.WorkspaceUsers.Returns(WorkspaceUsers);
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

    private static AiProposedChange CreateChange(Guid changeSetId, string toolName, long id = 1)
    {
        return new AiProposedChange
        {
            Id = id,
            ChangeSetId = changeSetId,
            Sequence = (int)id,
            ToolName = toolName,
            EntityType = "task",
            RefKey = "ref:1",
            Summary = "Create task",
            Payload = JsonDocument.Parse("""{"name":"Test","projectId":1}"""),
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
