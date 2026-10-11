using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages.Commands;

public class RestorePageVersionCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int PageId = 42;
    private const int VersionId = 5;
    private const string UserId = "user-1";
    private const string WorkspaceKey = "workspace";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly RestorePageVersionCommandHandler Handler;

    public RestorePageVersionCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);

        UnitOfWork.InvokeTransaction<bool>();
        GivenPage(PageKind.Document);

        UnitOfWork.PageVersions
            .GetViewModel(VersionId, PageId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new PageVersionViewModel { Id = VersionId, PageId = PageId, Title = "Old title", Body = "old body" });

        UnitOfWork.Pages.TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>()).Returns(true);

        UnitOfWork.Pages
            .GetViewModel(PageId, WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(new PageViewModel { Id = PageId, Title = "Old title", Slug = "rfc", Revision = 8 });

        Handler = new RestorePageVersionCommandHandler(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldWriteTheVersionContentAndARestorePoint()
    {
        var result = await Send();

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Pages.Received(1).TrySaveContent(
            Arg.Is<PageContentUpdate>(update =>
                update.ExpectedRevision == 7 &&
                update.Title == "Old title" &&
                update.Body == "old body" &&
                update.RestoredFromVersionId == VersionId),
            Arg.Any<CancellationToken>());

        await UnitOfWork.PageVersions.Received(1).AddAsync(
            Arg.Is<PageVersion>(version => version.RestoredFromVersionId == VersionId && version.Revision == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMoveTheSlugToTheRestoredTitle_KeepingItsId()
    {
        await Send();

        await UnitOfWork.Pages.Received(1).TrySaveContent(
            Arg.Is<PageContentUpdate>(update => update.Slug == "old-title-abcdefGHIJ12"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldIndexThePagesBelow_WhenTheVersionHadAnotherTitle()
    {
        UnitOfWork.Pages.GetSubtreeIds(PageId, WorkspaceId, false, Arg.Any<CancellationToken>()).Returns([PageId, 43]);

        await Send();

        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Index, PageId, 43);
    }

    [Fact]
    public async Task Handle_ShouldRestoreNoBody_WhenThePageIsAFolder()
    {
        GivenPage(PageKind.Folder);

        await Send();

        await UnitOfWork.Pages.Received(1).TrySaveContent(
            Arg.Is<PageContentUpdate>(update => update.Body == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenThePageChangedInBetween()
    {
        UnitOfWork.Pages.TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Send();

        result.IsConflict.Should().BeTrue();
        await UnitOfWork.PageVersions.DidNotReceive().AddAsync(Arg.Any<PageVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenThePageIsNotAtTheExpectedRevision()
    {
        var command = new RestorePageVersionCommand(PageId, VersionId, ExpectedRevision: 6);

        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsConflict.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRestore_WhenThePageIsAtTheExpectedRevision()
    {
        var command = new RestorePageVersionCommand(PageId, VersionId, ExpectedRevision: 7);

        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheVersionBelongsElsewhere()
    {
        UnitOfWork.PageVersions
            .GetViewModel(VersionId, PageId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns((PageVersionViewModel?)null);

        var result = await Send();

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().TrySaveContent(Arg.Any<PageContentUpdate>(), Arg.Any<CancellationToken>());
    }

    private void GivenPage(PageKind kind)
    {
        UnitOfWork.Pages
            .GetLiveInWorkspace(PageId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = PageId, WorkspaceId = WorkspaceId, Kind = kind, Title = "RFC", Slug = "rfc-abcdefGHIJ12", Revision = 7 });
    }

    private Task<ClientResponse<PageViewModel>> Send()
    {
        return Handler.Handle(new RestorePageVersionCommand(PageId, VersionId), TestContext.Current.CancellationToken).AsTask();
    }
}
