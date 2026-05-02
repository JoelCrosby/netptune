using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tags.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Commands;

public class DeleteTagsCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly DeleteTagsCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTagsCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var tagNames = new List<string> { "tag" };
        var tags = new List<Tag> { AutoFixtures.Tag };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(1, tagNames, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(tags);

        var result = await Handler.Handle(new DeleteTagsCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var tagNames = new List<string> { "tag" };
        var tags = new List<Tag> { AutoFixtures.Tag };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(1, tagNames, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(tags);

        await Handler.Handle(new DeleteTagsCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var tagNames = new List<string> { "tag" };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteTagsCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
