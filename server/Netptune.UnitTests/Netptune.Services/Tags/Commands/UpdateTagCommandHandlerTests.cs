using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tags.Commands.UpdateTag;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Commands;

public class UpdateTagCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateTagCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateTagCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();
        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(tag);

        var result = await Handler.Handle(new UpdateTagCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.NewValue);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(AutoFixtures.Tag);

        await Handler.Handle(new UpdateTagCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();

        var result = await Handler.Handle(new UpdateTagCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTagNotFound()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).ReturnsNull();

        var result = await Handler.Handle(new UpdateTagCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldTrimWhitespace_FromNewNameValue()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create() with
        {
            NewValue = "  Spaces before and after ",
        };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(AutoFixtures.Tag);

        var result = await Handler.Handle(new UpdateTagCommand(request), CancellationToken.None);

        result.Payload!.Name.Should().Be("Spaces before and after");
    }
}
