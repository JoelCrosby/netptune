using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.Boards.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Queries;

public class GetBoardQueryHandlerTests
{
    private readonly GetBoardQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetBoardQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetBoard_ShouldReturnCorrectly_WhenInputValid()
    {
        var board = AutoFixtures.Board;
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(board);

        var result = await Handler.Handle(new GetBoardQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBoard_ShouldReturnFailure_WhenNotFound()
    {
        UnitOfWork.Boards.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new GetBoardQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
