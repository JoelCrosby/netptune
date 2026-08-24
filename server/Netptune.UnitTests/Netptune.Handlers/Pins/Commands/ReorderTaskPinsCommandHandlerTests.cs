using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Pins.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pins.Commands;

public class ReorderTaskPinsCommandHandlerTests
{
    private const int WorkspaceId = 123;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ITaskPinRepository TaskPins = Substitute.For<ITaskPinRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly ReorderTaskPinsCommandHandler Handler;

    public ReorderTaskPinsCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.TryGetWorkspaceKey().Returns("workspace");

        GrantRole(WorkspaceRole.Member);

        Handler = new ReorderTaskPinsCommandHandler(UnitOfWork, TaskPins, Identity, PermissionCache);
    }

    [Fact]
    public async Task Handle_ShouldWriteEverySortOrder()
    {
        var first = Pin(1, TaskPinScope.User, "user-1");
        var second = Pin(2, TaskPinScope.Board, "user-2");

        Stored(first, second);

        var result = await Send([new TaskPinOrder(1, -2d), new TaskPinOrder(2, 0.5d)]);

        result.IsSuccess.Should().BeTrue();
        first.SortOrder.Should().Be(-2d);
        second.SortOrder.Should().Be(0.5d);

        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotFound_WhenAPinIsMissing()
    {
        Stored(Pin(1, TaskPinScope.User, "user-1"));

        var result = await Send([new TaskPinOrder(1, 0d), new TaskPinOrder(2, 1d)]);

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenAPersonalPinBelongsToSomebodyElse()
    {
        Stored(Pin(1, TaskPinScope.User, "user-2"));

        var result = await Send([new TaskPinOrder(1, 0d)]);

        result.IsForbidden.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenTheCallerCannotWriteAtASharedScope()
    {
        GrantRole(WorkspaceRole.Viewer);
        Stored(Pin(1, TaskPinScope.Board, "user-2"));

        var result = await Send([new TaskPinOrder(1, 0d)]);

        result.IsForbidden.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenThereIsNothingToReorder()
    {
        var result = await Send([]);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private Task<ClientResponse> Send(List<TaskPinOrder> items)
    {
        var request = new ReorderTaskPinsRequest { Items = items };

        return Handler.Handle(new ReorderTaskPinsCommand(request), TestContext.Current.CancellationToken).AsTask();
    }

    private void Stored(params TaskPin[] pins)
    {
        TaskPins.GetByIds(Arg.Any<IReadOnlyCollection<int>>(), WorkspaceId, Arg.Any<CancellationToken>()).Returns(pins.ToList());
    }

    private static TaskPin Pin(int id, TaskPinScope scope, string createdByUserId)
    {
        return new TaskPin
        {
            Id = id,
            ProjectTaskId = 9,
            Scope = scope,
            ScopeEntityId = 77,
            WorkspaceId = WorkspaceId,
            CreatedByUserId = createdByUserId,
        };
    }

    private void GrantRole(WorkspaceRole role)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = role,
            Permissions = role == WorkspaceRole.Viewer ? [] : [NetptunePermissions.Boards.Update],
        });
    }
}
