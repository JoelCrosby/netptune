using System.Text.Json;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Constants;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.TaskViews.Commands;
using Netptune.Query.Model;
using Netptune.Query.Tasks;
using Netptune.Query.Views;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.TaskViews.Commands;

public class SaveTaskViewCommandHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ITaskViewRepository TaskViews = Substitute.For<ITaskViewRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly SaveTaskViewCommandHandler Handler;

    public SaveTaskViewCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(123);
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.GetWorkspaceKey().Returns("workspace");
        Identity.TryGetWorkspaceKey().Returns("workspace");

        GrantRole(WorkspaceRole.Member);

        var referenceValidator = new TaskReferenceValidator(UnitOfWork);

        Handler = new SaveTaskViewCommandHandler(UnitOfWork, Identity, TaskViews, PermissionCache, referenceValidator);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheNameIsBlank()
    {
        var result = await Send(Request() with { Name = "  " });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A name is required.");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheQueryIsInvalid()
    {
        var definition = new TaskViewDefinition
        {
            Query = new QueryGroup
            {
                Conditions = [new QueryCondition { Field = "task.colour", Operator = QueryOperator.Equals, Values = ["blue"] }],
            },
        };
        var result = await Send(Request() with { Definition = definition });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not a known task field");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheNameIsAlreadyTaken()
    {
        TaskViews.NameExists(123, "Due soon", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Send(Request());

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A view named 'Due soon' already exists.");
    }

    [Fact]
    public async Task Handle_ShouldCreateAPrivateView_WhenTheCallerHasNoSharedRights()
    {
        var result = await Send(Request());

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be("Due soon");
        result.Payload.Slug.Should().StartWith("due-soon-");
        result.Payload.IsShared.Should().BeFalse();
        result.Payload.IsOwn.Should().BeTrue();

        await TaskViews.Received(1).AddAsync(Arg.Any<TaskView>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRefuseSharing_WhenTheCallerCannotManageSharedViews()
    {
        var result = await Send(Request() with { IsShared = true });

        result.IsForbidden.Should().BeTrue();

        await TaskViews.DidNotReceive().AddAsync(Arg.Any<TaskView>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAllowSharing_WhenTheCallerCanManageSharedViews()
    {
        GrantPermission(NetptunePermissions.TaskViews.ManageShared);

        var result = await Send(Request() with { IsShared = true });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsShared.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenEditingSomebodyElsesView()
    {
        TaskViews.GetInWorkspace(7, 123, false, Arg.Any<CancellationToken>()).Returns(Existing("user-2"));

        var result = await Send(Request() with { Id = 7 });

        result.IsForbidden.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldAllowEditingSomebodyElsesView_WhenTheCallerCanManageSharedViews()
    {
        GrantPermission(NetptunePermissions.TaskViews.ManageShared);
        TaskViews.GetInWorkspace(7, 123, false, Arg.Any<CancellationToken>()).Returns(Existing("user-2"));

        var result = await Send(Request() with { Id = 7 });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsOwn.Should().BeFalse();
        result.Payload.CanEdit.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNotFound_WhenTheViewDoesNotExist()
    {
        TaskViews.GetInWorkspace(9, 123, false, Arg.Any<CancellationToken>()).Returns((TaskView?)null);

        var result = await Send(Request() with { Id = 9 });

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldGiveTwoViewsOfTheSameName_DistinctSlugs()
    {
        var first = await Send(Request());
        var second = await Send(Request());

        first.Payload!.Slug.Should().NotBe(second.Payload!.Slug);
        first.Payload.Slug.Should().StartWith("due-soon-");
        second.Payload.Slug.Should().StartWith("due-soon-");
    }

    [Fact]
    public async Task Handle_ShouldLeaveTheSlugAlone_WhenAViewIsRenamed()
    {
        var existing = Existing("user-1");

        TaskViews.GetInWorkspace(7, 123, false, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Send(Request() with { Id = 7, Name = "Renamed" });

        result.Payload!.Name.Should().Be("Renamed");
        result.Payload.Slug.Should().Be("theirs", "an existing link must keep resolving after a rename");
    }

    [Fact]
    public async Task Handle_ShouldClampThePageSize_ToTheMaximum()
    {
        var definition = ValidDefinition() with { Display = new TaskViewDisplay { PageSize = 5000 } };
        var result = await Send(Request() with { Definition = definition });

        result.Payload!.Definition!.Display.PageSize.Should().Be(100);
        result.Payload.Definition.Version.Should().Be(TaskViewDefinition.CurrentVersion);
    }

    private Task<ClientResponse<TaskViewViewModel>> Send(SaveTaskViewRequest request)
    {
        return Handler.Handle(new SaveTaskViewCommand(request), TestContext.Current.CancellationToken).AsTask();
    }

    private static SaveTaskViewRequest Request()
    {
        return new SaveTaskViewRequest
        {
            Name = "Due soon",
            Definition = ValidDefinition(),
        };
    }

    private static TaskViewDefinition ValidDefinition()
    {
        return new TaskViewDefinition
        {
            Query = new QueryGroup
            {
                Conditions =
                [
                    new QueryCondition
                    {
                        Field = TaskFieldKeys.DueDate,
                        Operator = QueryOperator.InNextDays,
                        Values = ["7"],
                    },
                ],
            },
        };
    }

    private static TaskView Existing(string ownerId)
    {
        return new TaskView
        {
            Id = 7,
            Name = "Theirs",
            Slug = "theirs",
            WorkspaceId = 123,
            CreatedByUserId = ownerId,
            Definition = JsonSerializer.SerializeToDocument(ValidDefinition(), JsonOptions.Default),
        };
    }

    private void GrantRole(WorkspaceRole role)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = role,
            Permissions = [],
        });
    }

    private void GrantPermission(string permission)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = WorkspaceRole.Member,
            Permissions = [permission],
        });
    }
}
