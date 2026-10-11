using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
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
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
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

        Grant(NetptunePermissions.Sprints.Read, NetptunePermissions.Tasks.Read);

        Handler = new CreatePageCommandHandler(UnitOfWork, Identity, Activity, EventPublisher, PermissionCache);
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
    public async Task Handle_ShouldStartTheDocumentWithTheGivenBody()
    {
        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, Title = "Runbook", Body = "# Steps" });

        result.IsSuccess.Should().BeTrue();
        AddedPage!.Body.Should().Be("# Steps");
        await UnitOfWork.PageVersions.Received(1).AddAsync(
            Arg.Is<PageVersion>(version => version.Body == "# Steps" && version.Revision == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAFolderIsGivenABody()
    {
        var result = await Send(new CreatePageRequest { Kind = PageKind.Folder, Title = "RFCs", Body = "x" });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Folders have no body.");
    }

    [Fact]
    public async Task Handle_ShouldStartFromTheTemplate_WhenNoTitleOrBodyIsGiven()
    {
        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, TemplateKey = "retro" });

        result.IsSuccess.Should().BeTrue();
        AddedPage!.Title.Should().Be("Retro");
        AddedPage.Body.Should().Contain("## What went well");
    }

    [Theory]
    [InlineData("nope", PageKind.Document, null)]
    [InlineData("retro", PageKind.Folder, null)]
    [InlineData(PageTemplates.FromSprint, PageKind.Document, null)]
    public async Task Handle_ShouldFail_WhenTheTemplateCannotBeUsed(string key, PageKind kind, int? sprintId)
    {
        var result = await Send(new CreatePageRequest { Kind = kind, TemplateKey = key, SprintId = sprintId });

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.Pages.DidNotReceive().AddAsync(Arg.Any<Page>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRefuseReleaseNotes_WhenTheCallerCannotReadSprints()
    {
        Grant(NetptunePermissions.Tasks.Read);

        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, TemplateKey = PageTemplates.FromSprint, SprintId = 3 });

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.Sprints.DidNotReceive().GetSprintDetailAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldStartAPageInAProjectSpace_WhenTheProjectIsInTheWorkspace()
    {
        UnitOfWork.Projects
            .GetAsync(9, true, Arg.Any<CancellationToken>())
            .Returns(new Project { Id = 9, WorkspaceId = WorkspaceId, Name = "Mobile", Key = "MOB" });

        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, ProjectId = 9 });

        result.IsSuccess.Should().BeTrue();
        AddedPage!.ProjectId.Should().Be(9);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheProjectIsInAnotherWorkspace()
    {
        UnitOfWork.Projects
            .GetAsync(9, true, Arg.Any<CancellationToken>())
            .Returns(new Project { Id = 9, WorkspaceId = 99, Name = "Other", Key = "OTH" });

        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, ProjectId = 9 });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("The project does not exist.");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreTheRequestedProject_WhenThePageHasAParent()
    {
        var result = await Send(new CreatePageRequest { Kind = PageKind.Document, ParentId = ParentId, ProjectId = 42 });

        result.IsSuccess.Should().BeTrue();
        AddedPage!.ProjectId.Should().Be(3, "a page joins its parent's space");
        await UnitOfWork.Projects.DidNotReceive().GetAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
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

    private void Grant(params string[] permissions)
    {
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(new UserPermissions
        {
            UserId = UserId,
            WorkspaceKey = WorkspaceKey,
            Role = WorkspaceRole.Member,
            Permissions = [.. permissions],
        });
    }
}
