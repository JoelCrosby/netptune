using AutoFixture;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Requests;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Workspaces.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Workspaces.Commands;

public class UpdateWorkspaceCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly IWorkspaceUserCache WorkspaceUsers = Substitute.For<IWorkspaceUserCache>();
    private readonly IWorkspacePermissionCache WorkspacePermissions = Substitute.For<IWorkspacePermissionCache>();
    private readonly IWorkspaceCache WorkspaceCache = Substitute.For<IWorkspaceCache>();

    public UpdateWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, EventRecords, WorkspaceUsers, WorkspacePermissions, WorkspaceCache);

        UnitOfWork.Users.GetWorkspaceUserIds(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Without(item => item.NewSlug).Create();
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Workspace.Name.Should().Be(request.Name);
        result.Payload.Workspace.Description.Should().Be(request.Description);
        result.Payload.Workspace.Slug.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Without(item => item.NewSlug).Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldRenameTheWorkspace_WhenNewSlugIsAvailable()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            NewSlug = "New Workspace",
        };
        var workspace = AutoFixtures.Workspace with { Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);
        UnitOfWork.Workspaces.Exists("new-workspace", TestContext.Current.CancellationToken).Returns(false);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Workspace.Slug.Should().Be("new-workspace");
        result.Payload.PreviousSlug.Should().Be("workspace");
    }

    [Fact]
    public async Task Update_ShouldNotReportAPreviousSlug_WhenTheSlugIsUnchanged()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            Name = "Updated workspace",
        };
        var workspace = AutoFixtures.Workspace with { Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.PreviousSlug.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldEmitIdentifierChange_WhenTheSlugChanges()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            NewSlug = "renamed-workspace",
        };
        var workspace = AutoFixtures.Workspace with { Id = 42, Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<WorkspaceSettingsChangedPayload>>(eventRequest =>
                eventRequest.Payload.Fields.SequenceEqual(new[] { "identifier" })),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldForgetCachedMembershipUnderTheOldSlug_WhenTheSlugChanges()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            NewSlug = "renamed-workspace",
        };
        var workspace = AutoFixtures.Workspace with { Id = 42, Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);
        UnitOfWork.Users.GetWorkspaceUserIds(workspace.Id, TestContext.Current.CancellationToken)
            .Returns(["user-one", "user-two"]);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        WorkspaceUsers.Received(1).Remove(
            Arg.Is<WorkspaceUserKey>(key => key.WorkspaceKey == "workspace" && key.UserId == "user-one"));
        WorkspaceUsers.Received(1).Remove(
            Arg.Is<WorkspaceUserKey>(key => key.WorkspaceKey == "workspace" && key.UserId == "user-two"));
        WorkspacePermissions.Received(1).Remove(
            Arg.Is<WorkspaceUserKey>(key => key.WorkspaceKey == "workspace" && key.UserId == "user-one"));
        WorkspacePermissions.Received(1).Remove(
            Arg.Is<WorkspaceUserKey>(key => key.WorkspaceKey == "workspace" && key.UserId == "user-two"));
        WorkspaceCache.Received(1).Remove("workspace");
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTheNewSlugIsTaken()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            NewSlug = "taken-workspace",
        };
        var workspace = AutoFixtures.Workspace with { Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);
        UnitOfWork.Workspaces.Exists("taken-workspace", TestContext.Current.CancellationToken).Returns(true);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
        workspace.Slug.Should().Be("workspace");
        await UnitOfWork.DidNotReceive().CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTheNewSlugNormalisesTooShort()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            NewSlug = "!!!",
        };
        var workspace = AutoFixtures.Workspace with { Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        workspace.Slug.Should().Be("workspace");
    }

    [Fact]
    public async Task Update_ShouldNotTreatAnUnchangedSlugAsARename()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            NewSlug = "workspace",
            Name = "Updated workspace",
        };
        var workspace = AutoFixtures.Workspace with { Id = 42, Slug = "workspace", Name = "Original workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<WorkspaceSettingsChangedPayload>>(eventRequest =>
                eventRequest.Payload.Fields.SequenceEqual(new[] { "name" })),
            TestContext.Current.CancellationToken);
        WorkspaceUsers.DidNotReceive().Remove(Arg.Any<WorkspaceUserKey>());
    }

    [Fact]
    public async Task Update_ShouldEmitWorkspaceSettingsChanged_WhenValuesChange()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            Name = "Updated workspace",
            IsPublic = true,
        };
        var workspace = AutoFixtures.Workspace with
        {
            Id = 42,
            Name = "Original workspace",
            IsPublic = false,
        };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<WorkspaceSettingsChangedPayload>>(eventRequest =>
                eventRequest.EventKey == EventKeys.WorkspaceSettingsChanged &&
                eventRequest.WorkspaceId == workspace.Id &&
                eventRequest.Payload.Fields.SequenceEqual(new[] { "name", "visibility" })),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldDropPublicPermissionsOutsideTheCeiling()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            PublicPermissions =
            [
                NetptunePermissions.Tasks.Read,
                NetptunePermissions.Members.Read,
                NetptunePermissions.Tasks.Delete,
            ],
        };
        var workspace = AutoFixtures.Workspace with { IsPublic = true };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.Workspace.PublicPermissions.Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
    }

    [Fact]
    public async Task Update_ShouldStoreTheDefaultSelection_WhenAWorkspaceFirstBecomesPublic()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            IsPublic = true,
        };
        var workspace = AutoFixtures.Workspace with { IsPublic = false };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.Workspace.PublicPermissions.Should().BeEquivalentTo(NetptunePermissions.PublicReadable);
    }

    [Fact]
    public async Task Update_ShouldKeepTheStoredSelection_WhenTheRequestOmitsIt()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            Name = "Renamed workspace",
        };
        var workspace = AutoFixtures.Workspace with
        {
            IsPublic = true,
            PublicPermissions = [NetptunePermissions.Tasks.Read],
        };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.Workspace.PublicPermissions.Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
    }

    [Fact]
    public async Task Update_ShouldEmitPublicAccessChange_WhenTheSelectionChanges()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            PublicPermissions = [NetptunePermissions.Tasks.Read],
        };
        var workspace = AutoFixtures.Workspace with
        {
            Id = 42,
            IsPublic = true,
            PublicPermissions = [NetptunePermissions.Tasks.Read, NetptunePermissions.Sprints.Read],
        };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<WorkspaceSettingsChangedPayload>>(eventRequest =>
                eventRequest.Payload.Fields.SequenceEqual(new[] { "public_access" })),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldClampTheUploadLimit_WhenTheRequestedSizeIsOutOfRange()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            MaxUploadBytes = UploadLimits.MaximumMaxUploadBytes * 4,
        };
        var workspace = AutoFixtures.Workspace with { MaxUploadBytes = UploadLimits.DefaultMaxUploadBytes };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.Workspace.MaxUploadBytes.Should().Be(UploadLimits.MaximumMaxUploadBytes);
    }

    [Fact]
    public async Task Update_ShouldKeepTheStoredUploadLimit_WhenTheRequestOmitsIt()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            Name = "Renamed workspace",
        };
        var workspace = AutoFixtures.Workspace with { MaxUploadBytes = 10L * 1024 * 1024 };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.Workspace.MaxUploadBytes.Should().Be(10L * 1024 * 1024);
    }

    [Fact]
    public async Task Update_ShouldEmitUploadLimitChange_WhenTheLimitChanges()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            MaxUploadBytes = 100L * 1024 * 1024,
        };
        var workspace = AutoFixtures.Workspace with
        {
            Id = 42,
            MaxUploadBytes = UploadLimits.DefaultMaxUploadBytes,
        };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<WorkspaceSettingsChangedPayload>>(eventRequest =>
                eventRequest.Payload.Fields.SequenceEqual(new[] { "upload_limit" })),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldForgetTheCachedWorkspace_WhenTheSlugIsUnchanged()
    {
        var request = new UpdateWorkspaceRequest
        {
            Slug = "workspace",
            MaxUploadBytes = 100L * 1024 * 1024,
        };
        var workspace = AutoFixtures.Workspace with { Slug = "workspace" };

        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(
                request.Slug,
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        WorkspaceCache.Received(1).Remove("workspace");
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Without(item => item.NewSlug).Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
