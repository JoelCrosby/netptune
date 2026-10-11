using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages.Commands;

public class UpdatePageCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int PageId = 42;
    private const string UserId = "user-1";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly Page TaggedPage;
    private readonly UpdatePageCommandHandler Handler;

    public UpdatePageCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);

        TaggedPage = new Page
        {
            Id = PageId,
            WorkspaceId = WorkspaceId,
            Title = "RFC",
            Slug = "rfc",
            PageTags = [new PageTag { Id = 1, PageId = PageId, TagId = 10 }, new PageTag { Id = 2, PageId = PageId, TagId = 11 }],
        };

        UnitOfWork.Pages.GetLiveWithTags(PageId, WorkspaceId, Arg.Any<CancellationToken>()).Returns(TaggedPage);

        UnitOfWork.Tags
            .GetExistingIds(Arg.Any<IReadOnlyCollection<int>>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<int>>().Where(id => id < 100).ToList());

        UnitOfWork.Pages
            .GetViewModel(PageId, WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(new PageViewModel { Id = PageId, Title = "RFC", Slug = "rfc" });

        Handler = new UpdatePageCommandHandler(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Handle_ShouldKeepExistingJoinRowsAndAddNewOnes_WhenTagsChange()
    {
        var result = await Send(new UpdatePageRequest { TagIds = [11, 12] });

        result.IsSuccess.Should().BeTrue();
        TaggedPage.PageTags.Select(pageTag => pageTag.TagId).Should().BeEquivalentTo([11, 12]);
        TaggedPage.PageTags.Single(pageTag => pageTag.TagId == 11).Id.Should().Be(2);
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDropTagIdsThatAreNotLiveInTheWorkspace()
    {
        await Send(new UpdatePageRequest { TagIds = [10, 500] });

        TaggedPage.PageTags.Select(pageTag => pageTag.TagId).Should().Equal(10);
    }

    [Fact]
    public async Task Handle_ShouldLeaveTagsAlone_WhenOnlyTheStatusChanges()
    {
        await Send(new UpdatePageRequest { Status = PageStatus.Published });

        TaggedPage.Status.Should().Be(PageStatus.Published);
        TaggedPage.PageTags.Should().HaveCount(2);
        await UnitOfWork.Tags.DidNotReceive().GetExistingIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheStatusIsUnknown()
    {
        var result = await Send(new UpdatePageRequest { Status = (PageStatus)9 });

        result.Message.Should().Be("Unknown page status.");
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenThePageIsNotLive()
    {
        UnitOfWork.Pages.GetLiveWithTags(PageId, WorkspaceId, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send(new UpdatePageRequest { Status = PageStatus.Draft });

        result.IsNotFound.Should().BeTrue();
    }

    private Task<ClientResponse<PageViewModel>> Send(UpdatePageRequest request)
    {
        return Handler.Handle(new UpdatePageCommand(PageId, request), TestContext.Current.CancellationToken).AsTask();
    }
}
