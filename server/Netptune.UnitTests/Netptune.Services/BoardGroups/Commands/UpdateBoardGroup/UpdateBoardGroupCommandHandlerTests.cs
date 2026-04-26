using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.BoardGroups.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.BoardGroups.Commands;

public class UpdateBoardGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateBoardGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardGroupRequest>().Create();
        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        var result = await Handler.Handle(new UpdateBoardGroupCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(AutoFixtures.BoardGroup);

        await Handler.Handle(new UpdateBoardGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture.Build<UpdateBoardGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
