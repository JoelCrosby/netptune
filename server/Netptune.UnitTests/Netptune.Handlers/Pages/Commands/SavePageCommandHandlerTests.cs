using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
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

public class SavePageCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int PageId = 42;
    private const string UserId = "user-1";
    private const string WorkspaceKey = "workspace";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly SavePageCommandHandler Handler;

    public SavePageCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);

        UnitOfWork.InvokeTransaction<bool>();
        GivenPage(PageKind.Document);

        UnitOfWork.Pages
            .GetViewModel(PageId, WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(new PageViewModel { Id = PageId, Title = "Current", Slug = "current", Revision = 4 });

        UnitOfWork.Pages.TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>()).Returns(true);

        Handler = new SavePageCommandHandler(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldSaveWithTheExpectedRevision_WhenTheRevisionMatches()
    {
        var result = await Send(new SavePageRequest { Title = "  Roadmap  ", Body = "# Hello", Revision = 3 });

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Pages.Received(1).TrySaveContent(
            Arg.Is<PageContentUpdate>(update =>
                update.PageId == PageId &&
                update.ExpectedRevision == 3 &&
                update.Title == "Roadmap" &&
                update.Body == "# Hello" &&
                update.UserId == UserId),
            Arg.Any<CancellationToken>());

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
        await UnitOfWork.PageVersions.Received(1).AddAsync(
            Arg.Is<PageVersion>(version => version.PageId == PageId && version.Revision == 4 && version.Body == "# Hello"),
            Arg.Any<CancellationToken>());
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFoldIntoTheLatestVersion_WhenTheSameAuthorSavedMomentsAgo()
    {
        var latest = new PageVersion { Id = 9, PageId = PageId, Revision = 3, Title = "Old", Body = "old", CreatedByUserId = UserId, CreatedAt = DateTime.UtcNow.AddMinutes(-2) };

        UnitOfWork.PageVersions.GetLatest(PageId, Arg.Any<CancellationToken>()).Returns(latest);

        await Send(new SavePageRequest { Title = "Roadmap", Body = "new", Revision = 3 });

        latest.Revision.Should().Be(4);
        latest.Body.Should().Be("new");
        await UnitOfWork.PageVersions.DidNotReceive().AddAsync(Arg.Any<PageVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotRecordAVersion_WhenTheRevisionIsStale()
    {
        UnitOfWork.Pages.TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>()).Returns(false);

        await Send(new SavePageRequest { Title = "Roadmap", Body = "mine", Revision = 3 });

        await UnitOfWork.PageVersions.DidNotReceive().AddAsync(Arg.Any<PageVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictWithTheCurrentPage_WhenTheRevisionIsStale()
    {
        UnitOfWork.Pages.TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Send(new SavePageRequest { Title = "Roadmap", Body = "mine", Revision = 3 });

        result.IsConflict.Should().BeTrue();
        result.Message.Should().Be(SavePageCommandHandler.ConflictMessage);
        result.Payload!.Revision.Should().Be(4);
        Activity.DidNotReceive().Log(Arg.Any<Action<ActivityOptions>>());
        await EventPublisher.DidNotReceivePageIndexEvent();
    }

    [Fact]
    public async Task Handle_ShouldIndexOnlyThePage_WhenTheTitleIsUnchanged()
    {
        await Send(new SavePageRequest { Title = "Current", Body = "edited", Revision = 3 });

        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Index, PageId);
        await UnitOfWork.Pages.DidNotReceive().GetSubtreeIds(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldIndexThePagesBelow_WhenThePageIsRenamed()
    {
        UnitOfWork.Pages.GetSubtreeIds(PageId, WorkspaceId, false, Arg.Any<CancellationToken>()).Returns([PageId, 43, 44]);

        await Send(new SavePageRequest { Title = "Renamed", Body = "edited", Revision = 3 });

        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Index, PageId, 43, 44);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheBodyIsOverTheLimit()
    {
        var body = new string('a', PageBody.MaxBytes + 1);

        var result = await Send(new SavePageRequest { Title = "Big", Body = body, Revision = 3 });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PageBody.TooLargeMessage);
        await UnitOfWork.Pages.DidNotReceive().TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCountBytesNotCharacters_WhenCheckingTheLimit()
    {
        var multiByteCharacters = PageBody.MaxBytes / 2;
        var body = new string('é', multiByteCharacters + 1);

        var result = await Send(new SavePageRequest { Title = "Accents", Body = body, Revision = 3 });

        result.Message.Should().Be(PageBody.TooLargeMessage);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAFolderIsGivenABody()
    {
        GivenPage(PageKind.Folder);

        var result = await Send(new SavePageRequest { Title = "Engineering", Body = "text", Revision = 3 });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Folders have no body.");
    }

    [Fact]
    public async Task Handle_ShouldDefaultTheTitle_WhenTheTitleIsBlank()
    {
        await Send(new SavePageRequest { Title = "   ", Body = "", Revision = 3 });

        await UnitOfWork.Pages.Received(1).TrySaveContent(
            Arg.Is<PageContentUpdate>(update => update.Title == PageTitle.Untitled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenThePageIsNotLiveInTheWorkspace()
    {
        UnitOfWork.Pages.GetLiveInWorkspace(PageId, WorkspaceId, true, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send(new SavePageRequest { Title = "Roadmap", Revision = 3 });

        result.IsNotFound.Should().BeTrue();
    }

    private void GivenPage(PageKind kind)
    {
        UnitOfWork.Pages
            .GetLiveInWorkspace(PageId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = PageId, WorkspaceId = WorkspaceId, Kind = kind, Title = "Current", Slug = "current", Revision = 3 });
    }

    private Task<ClientResponse<PageViewModel>> Send(SavePageRequest request)
    {
        return Handler.Handle(new SavePageCommand(PageId, request), TestContext.Current.CancellationToken).AsTask();
    }
}
