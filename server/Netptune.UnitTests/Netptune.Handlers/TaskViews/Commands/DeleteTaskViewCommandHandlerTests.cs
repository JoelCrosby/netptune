using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.TaskViews.Commands;
using Netptune.Query.Views;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.TaskViews.Commands;

public class DeleteTaskViewCommandHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ITaskViewRepository TaskViews = Substitute.For<ITaskViewRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly DeleteTaskViewCommandHandler Handler;

    public DeleteTaskViewCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(123);
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.TryGetWorkspaceKey().Returns("workspace");

        GrantPermissions([]);

        Handler = new DeleteTaskViewCommandHandler(UnitOfWork, Identity, TaskViews, PermissionCache);
    }

    [Fact]
    public async Task Handle_ShouldNotFound_WhenTheViewIsMissing()
    {
        TaskViews.GetBySlug("a-view-x7k2m9p3q1r5", 123, false, Arg.Any<CancellationToken>()).Returns((TaskView?)null);

        var result = await Send();

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldDelete_WhenTheViewIsOwnedByTheCaller()
    {
        var view = Existing("user-1");

        TaskViews.GetBySlug("a-view-x7k2m9p3q1r5", 123, false, Arg.Any<CancellationToken>()).Returns(view);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        view.IsDeleted.Should().BeTrue();
        view.DeletedByUserId.Should().Be("user-1");

        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenTheViewBelongsToSomebodyElse()
    {
        var view = Existing("user-2");

        TaskViews.GetBySlug("a-view-x7k2m9p3q1r5", 123, false, Arg.Any<CancellationToken>()).Returns(view);

        var result = await Send();

        result.IsForbidden.Should().BeTrue();
        view.IsDeleted.Should().BeFalse();

        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDeleteSomebodyElsesView_WhenTheCallerCanManageSharedViews()
    {
        GrantPermissions([NetptunePermissions.TaskViews.ManageShared]);

        var view = Existing("user-2");

        TaskViews.GetBySlug("a-view-x7k2m9p3q1r5", 123, false, Arg.Any<CancellationToken>()).Returns(view);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        view.IsDeleted.Should().BeTrue();
    }

    private Task<ClientResponse> Send()
    {
        return Handler.Handle(new DeleteTaskViewCommand("a-view-x7k2m9p3q1r5"), TestContext.Current.CancellationToken).AsTask();
    }

    private static TaskView Existing(string ownerId)
    {
        return new TaskView
        {
            Id = 7,
            Name = "A view",
            Slug = "a-view-x7k2m9p3q1r5",
            WorkspaceId = 123,
            CreatedByUserId = ownerId,
            Definition = JsonSerializer.SerializeToDocument(new TaskViewDefinition(), JsonOptions.Default),
        };
    }

    private void GrantPermissions(HashSet<string> permissions)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = WorkspaceRole.Member,
            Permissions = permissions,
        });
    }
}
