using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.Workspaces.Queries.IsWorkspaceSlugUnique;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Queries;

public class IsWorkspaceSlugUniqueQueryHandlerTests
{
    private readonly IsWorkspaceSlugUniqueQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public IsWorkspaceSlugUniqueQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenUnique()
    {
        UnitOfWork.Workspaces.Exists(Arg.Any<string>()).Returns(false);

        var result = await Handler.Handle(new IsWorkspaceSlugUniqueQuery("slug"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload?.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnFailure_WhenNotUnique()
    {
        UnitOfWork.Workspaces.Exists(Arg.Any<string>()).Returns(true);

        var result = await Handler.Handle(new IsWorkspaceSlugUniqueQuery("slug"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload?.IsUnique.Should().BeFalse();
    }
}
