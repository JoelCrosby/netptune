using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Queries;

public class GetAllUsersQueryHandlerTests
{
    private readonly GetAllUsersQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetAllUsersQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        UnitOfWork.Users.GetAllAsync().Returns(new List<AppUser> { AutoFixtures.AppUser });

        var result = await Handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}
