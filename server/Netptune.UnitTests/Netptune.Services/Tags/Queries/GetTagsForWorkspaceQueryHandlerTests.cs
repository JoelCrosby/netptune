using AutoFixture;

using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Services.Tags.Queries.GetTagsForWorkspace;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Queries;

public class GetTagsForWorkspaceQueryHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly GetTagsForWorkspaceQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTagsForWorkspaceQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTagsForWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var tags = new List<TagViewModel> { Fixture.Create<TagViewModel>() };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetViewModelsForWorkspace(1).Returns(tags);

        var result = await Handler.Handle(new GetTagsForWorkspaceQuery(), CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTagsForWorkspace_ShouldReturnNull_WhenWorkspaceNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();

        var result = await Handler.Handle(new GetTagsForWorkspaceQuery(), CancellationToken.None);

        result.Should().BeNull();
    }
}
