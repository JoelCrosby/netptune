using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Services.Tags.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Commands;

public class CreateTagCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateTagCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateTagCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddTagRequest>().Create();
        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);

        var result = await Handler.Handle(new CreateTagCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddTagRequest>().Create();
        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);

        await Handler.Handle(new CreateTagCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<AddTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(false);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new CreateTagCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenTagAlreadyExists()
    {
        var request = Fixture.Build<AddTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(true);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);

        var result = await Handler.Handle(new CreateTagCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
