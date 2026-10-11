using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.Services.Search;
using Netptune.Handlers.Search.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Search.Queries;

public class SearchTaskIdsQueryHandlerTests
{
    private readonly IMeilisearchService Search = Substitute.For<IMeilisearchService>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly SearchTaskIdsQueryHandler Handler;

    public SearchTaskIdsQueryHandlerTests()
    {
        Handler = new SearchTaskIdsQueryHandler(Search, Identity);
        Identity.GetWorkspaceKey().Returns("netptune");
    }

    [Fact]
    public async Task Handle_ShouldSearchTheCurrentWorkspace_WithTheLimitCapped()
    {
        var expected = new TaskIdSearchResult([1, 2], 2);

        Search.SearchTaskIdsAsync(Arg.Any<TaskIdSearchQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await Handler.Handle(new SearchTaskIdsQuery("fares", 500, ProjectId: 3, AssigneeId: "user-1"), TestContext.Current.CancellationToken);

        result.Should().Be(expected);
        await Search.Received(1).SearchTaskIdsAsync(
            new TaskIdSearchQuery("fares", "netptune", SearchTaskIdsQueryHandler.MaximumLimit, 3, "user-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTheSearchIndexIsUnavailable()
    {
        Search.SearchTaskIdsAsync(Arg.Any<TaskIdSearchQuery>(), Arg.Any<CancellationToken>()).Returns((TaskIdSearchResult?)null);

        var result = await Handler.Handle(new SearchTaskIdsQuery("fares", 10), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }
}
