using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.BoardGroups.Queries.GetBoardGroup;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.BoardGroups.Queries;

public class GetBoardGroupQueryHandlerTests
{
    private readonly GetBoardGroupQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetBoardGroupQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var boardGroup = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns(boardGroup);

        var result = await Handler.Handle(new GetBoardGroupQuery(1), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(boardGroup);
    }
}
