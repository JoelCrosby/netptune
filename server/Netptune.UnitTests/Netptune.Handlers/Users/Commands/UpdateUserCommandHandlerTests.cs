using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Users.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private const string CurrentUserId = "current-user-id";

    private readonly Fixture Fixture = new();
    private readonly UpdateUserCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public UpdateUserCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(CurrentUserId);

        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = SelfRequest();
        var user = AutoFixtures.AppUser;

        UnitOfWork.Users.GetAsync(CurrentUserId, cancellationToken: TestContext.Current.CancellationToken).Returns(user);

        var result = await Handler.Handle(new UpdateUserCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Firstname.Should().Be(request.Firstname);
        result.Payload!.Lastname.Should().Be(request.Lastname);
        result.Payload!.PictureUrl.Should().Be(request.PictureUrl);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = SelfRequest();
        UnitOfWork.Users.GetAsync(CurrentUserId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.AppUser);

        await Handler.Handle(new UpdateUserCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = SelfRequest();
        UnitOfWork.Users.GetAsync(CurrentUserId, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateUserCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTargetIsAnotherUser()
    {
        var request = Fixture.Build<UpdateUserRequest>().With(x => x.Id, "another-user-id").Create();

        var result = await Handler.Handle(new UpdateUserCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ShouldNotLoadAnotherUser_WhenTargetIsAnotherUser()
    {
        var request = Fixture.Build<UpdateUserRequest>().With(x => x.Id, "another-user-id").Create();

        await Handler.Handle(new UpdateUserCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Users.Received(0).GetAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Received(0).CompleteAsync(Arg.Any<CancellationToken>());
    }

    private UpdateUserRequest SelfRequest()
    {
        return Fixture.Build<UpdateUserRequest>().With(x => x.Id, CurrentUserId).Create();
    }
}
