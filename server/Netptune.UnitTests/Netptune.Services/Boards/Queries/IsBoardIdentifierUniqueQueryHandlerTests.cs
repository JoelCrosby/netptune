using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.Boards.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Boards.Queries;

public class IsBoardIdentifierUniqueQueryHandlerTests
{
    private readonly IsBoardIdentifierUniqueQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public IsBoardIdentifierUniqueQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task IsIdentifierUnique_ShouldReturnCorrectly_WhenNotExists()
    {
        UnitOfWork.Boards.Exists("identifier").Returns(false);

        var result = await Handler.Handle(new IsBoardIdentifierUniqueQuery("identifier"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsIdentifierUnique_ShouldReturnCorrectly_WhenExists()
    {
        UnitOfWork.Boards.Exists("identifier").Returns(true);

        var result = await Handler.Handle(new IsBoardIdentifierUniqueQuery("identifier"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsUnique.Should().BeFalse();
    }
}
