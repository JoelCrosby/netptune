using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tags.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tags.Commands;

public class DeleteTagsCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly DeleteTagsCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();

    public DeleteTagsCommandHandlerTests()
    {
        UnitOfWork.Pages
            .GetIdsTaggedWith(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        Handler = new(UnitOfWork, Identity, Activity, EventPublisher);
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

        var result = await Handler.Handle(new DeleteTagsCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReindexThePagesThatCarriedTheTags()
    {
        var tagNames = new List<string> { "tag" };
        var tags = new List<Tag> { AutoFixtures.Tag };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(1, tagNames, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(tags);
        UnitOfWork.Pages
            .GetIdsTaggedWith(Arg.Any<IReadOnlyCollection<int>>(), 1, TestContext.Current.CancellationToken)
            .Returns([42]);

        await Handler.Handle(new DeleteTagsCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(message =>
            message.Operation == SearchIndexOperation.Index
            && message.EntityType == SearchIndexEntityTypes.Page
            && message.EntityIds.SequenceEqual(new[] { 42 })));
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

        await Handler.Handle(new DeleteTagsCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var tagNames = new List<string> { "tag" };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteTagsCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
