using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Users.Commands.UpdateUser;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateUserCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public UpdateUserCommandHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateUserRequest>().Create();
        var user = AutoFixtures.AppUser;

        UnitOfWork.Users.GetAsync(Arg.Any<string>()).Returns(user);

        var result = await Handler.Handle(new UpdateUserCommand(request), CancellationToken.None);

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
        var request = Fixture.Build<UpdateUserRequest>().Create();
        UnitOfWork.Users.GetAsync(Arg.Any<string>()).Returns(AutoFixtures.AppUser);

        await Handler.Handle(new UpdateUserCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = Fixture.Build<UpdateUserRequest>().Create();
        UnitOfWork.Users.GetAsync(Arg.Any<string>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
