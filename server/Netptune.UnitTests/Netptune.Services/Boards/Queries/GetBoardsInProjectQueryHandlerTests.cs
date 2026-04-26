using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Boards.Queries.GetBoardsInProject;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Queries;

public class GetBoardsInProjectQueryHandlerTests
{
    private readonly GetBoardsInProjectQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetBoardsInProjectQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetBoardsInProject_ShouldReturnCorrectly_WhenValidId()
    {
        var boards = new List<Board> { AutoFixtures.Board };

        UnitOfWork.Boards.GetBoardsInProject(1, Arg.Any<bool>()).Returns(boards);

        var result = await Handler.Handle(new GetBoardsInProjectQuery(1), CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}
