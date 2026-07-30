using AutoFixture;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Events;
using Netptune.Core.Services;
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

    public UpdateWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, EventRecords);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Create();
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Slug.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
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

        result.Payload!.PublicPermissions.Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
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

        result.Payload!.PublicPermissions.Should().BeEquivalentTo(NetptunePermissions.PublicReadable);
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

        result.Payload!.PublicPermissions.Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
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
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
