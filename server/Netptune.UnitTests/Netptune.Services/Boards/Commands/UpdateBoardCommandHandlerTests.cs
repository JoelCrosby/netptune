using AutoFixture;

using FluentAssertions;

using Netptune.Core.Encoding;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Boards.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Commands;

public class UpdateBoardCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateBoardCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        var board = AutoFixtures.Board;

        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(board);

        var result = await Handler.Handle(new UpdateBoardCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Identifier.Should().Be(request.Identifier!.ToUrlSlug());
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Board);

        await Handler.Handle(new UpdateBoardCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNotFound()
    {
        var request = Fixture.Build<UpdateBoardRequest>().Create();
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateBoardCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
