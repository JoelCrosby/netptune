using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
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

public class CreatePageCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int ParentId = 50;
    private const string UserId = "user-1";
    private const string WorkspaceKey = "workspace";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly CreatePageCommandHandler Handler;
    private Page? AddedPage;

    public CreatePageCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);
        UnitOfWork.InvokeTransaction();

        UnitOfWork.Pages
            .GetLiveInWorkspace(ParentId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = ParentId, WorkspaceId = WorkspaceId, ProjectId = 3, Title = "RFCs", Slug = "rfcs" });

        UnitOfWork.Pages.GetNextSortOrder(WorkspaceId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(4d);

        UnitOfWork.Pages
            .AddAsync(Arg.Do<Page>(page => AddedPage = page), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Page>());

        UnitOfWork.Pages
            .GetViewModel(Arg.Any<int>(), WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(new PageViewModel { Title = "Untitled", Slug = "untitled" });

        Handler = new CreatePageCommandHandler(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldCreateAnUntitledDraftAtTheEnd_WhenNoTitleIsGiven()
    {
        var result = await Send(new CreatePageRequest { Kind = PageKind.Document });

        result.IsSuccess.Should().BeTrue();
        AddedPage!.Title.Should().Be(PageTitle.Untitled);
        AddedPage.Status.Should().Be(PageStatus.Draft);
        AddedPage.Revision.Should().Be(1);
        AddedPage.SortOrder.Should().Be(4);
        AddedPage.Body.Should().BeEmpty();
        AddedPage.Slug.Should().StartWith("untitled-");
        AddedPage.CreatedByUserId.Should().Be(UserId);
        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Index, AddedPage.Id);
    }

    [Fact]
    public async Task Handle_ShouldInheritTheParentsSpace_WhenCreatedUnderAParent()
    {
        await Send(new CreatePageRequest { Kind = PageKind.Document, ParentId = ParentId, Title = "RFC-015" });

        AddedPage!.ParentId.Should().Be(ParentId);
        AddedPage.ProjectId.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldCreateAFolderWithoutABody()
    {
        await Send(new CreatePageRequest { Kind = PageKind.Folder, Title = "Runbooks" });

        AddedPage!.Kind.Should().Be(PageKind.Folder);
        AddedPage.Body.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheParentDoesNotExist()
    {
        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, ParentId = 999 });

        result.Message.Should().Be("The parent page does not exist.");
        AddedPage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheTitleIsTooLong()
    {
        var title = new string('a', PageTitle.MaxLength + 1);

        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, Title = title });

        result.Message.Should().Be(PageTitle.TooLongMessage);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheKindIsUnknown()
    {
        var result = await Send(new CreatePageRequest { Kind = (PageKind)9 });

        result.Message.Should().Be("Unknown page kind.");
    }

    private Task<ClientResponse<PageViewModel>> Send(CreatePageRequest request)
    {
        return Handler.Handle(new CreatePageCommand(request), TestContext.Current.CancellationToken).AsTask();
    }
}
