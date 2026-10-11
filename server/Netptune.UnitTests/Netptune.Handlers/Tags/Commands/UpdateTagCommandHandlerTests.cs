using AutoFixture;

using FluentAssertions;

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

public class UpdateTagCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateTagCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();

    public UpdateTagCommandHandlerTests()
    {
        UnitOfWork.Pages
            .GetIdsTaggedWith(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        Handler = new(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();
        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1, cancellationToken: TestContext.Current.CancellationToken).Returns(tag);

        var result = await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.NewValue);
    }

    [Fact]
    public async Task Update_ShouldReindexTheTaggedPages_WhenATagIsRenamed()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();
        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1, cancellationToken: TestContext.Current.CancellationToken).Returns(tag);
        UnitOfWork.Pages
            .GetIdsTaggedWith(
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { tag.Id })),
                1,
                TestContext.Current.CancellationToken)
            .Returns([42, 43]);

        await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(message =>
            message.Operation == SearchIndexOperation.Index
            && message.EntityType == SearchIndexEntityTypes.Page
            && message.EntityIds.SequenceEqual(new[] { 42, 43 })
            && message.WorkspaceSlug == "key"));
    }

    [Fact]
    public async Task Update_ShouldNotDispatch_WhenNoPageCarriesTheTag()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Tag);

        await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.DidNotReceive().Dispatch(Arg.Any<SearchIndexEvent>());
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Tag);

        await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTagNotFound()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldTrimWhitespace_FromNewNameValue()
    {
        var request = Fixture.Build<UpdateTagRequest>().Create() with
        {
            NewValue = "  Spaces before and after ",
        };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Tag);

        var result = await Handler.Handle(new UpdateTagCommand(request), TestContext.Current.CancellationToken);

        result.Payload!.Name.Should().Be("Spaces before and after");
    }
}
