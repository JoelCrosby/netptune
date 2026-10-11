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
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Pages;
using Netptune.Handlers.Pages.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages.Commands;

public class ImportPagesCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int FirstPageId = 100;
    private const string UserId = "user-1";
    private const string WorkspaceKey = "workspace";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly ImportPagesCommandHandler Handler;
    private List<Page> AddedPages = [];
    private List<PageLink> AddedLinks = [];

    public ImportPagesCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);
        UnitOfWork.InvokeTransaction();

        UnitOfWork.Pages.GetNextSortOrder(WorkspaceId, null, Arg.Any<CancellationToken>()).Returns(5d);

        UnitOfWork.Pages
            .AddRangeAsync(Arg.Do<IEnumerable<Page>>(AssignIds), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        UnitOfWork.PageLinks
            .AddRangeAsync(Arg.Do<IEnumerable<PageLink>>(links => AddedLinks = links.ToList()), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        Handler = new ImportPagesCommandHandler(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldPlaceTopLevelPagesAfterTheExistingSiblings()
    {
        var result = await Send(new ImportPagesRequest(), File("b.md", "B"), File("a.md", "A"));

        result.IsSuccess.Should().BeTrue();
        AddedPages.Select(page => (page.Title, page.SortOrder)).Should().Equal(("a", 5d), ("b", 6d));
        AddedPages.Should().OnlyContain(page => page.ParentId == null && page.Parent == null);
    }

    [Fact]
    public async Task Handle_ShouldNumberChildrenFromOne()
    {
        await Send(new ImportPagesRequest(), File("Guide/one.md", "1"), File("Guide/two.md", "2"));

        var folder = AddedPages.Single(page => page.Kind == PageKind.Folder);
        var children = AddedPages.Where(page => page.Parent == folder).ToList();

        folder.SortOrder.Should().Be(5);
        children.Select(child => child.SortOrder).Should().Equal(1d, 2d);
    }

    [Fact]
    public async Task Handle_ShouldReturnThePlacementError_WhenTheParentDoesNotExist()
    {
        var result = await Send(new ImportPagesRequest { ParentId = 999 }, File("a.md", "A"));

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PagePlacement.ParentMissingMessage);
        await UnitOfWork.Pages.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Page>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnTheReadError_WithoutTouchingTheWorkspace()
    {
        var upload = new PageImportRead { Error = "The zip file could not be read." };

        var result = await Handler.Handle(new ImportPagesCommand(new ImportPagesRequest(), upload), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("The zip file could not be read.");
        await Identity.DidNotReceive().GetWorkspaceId();
        await UnitOfWork.Pages.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Page>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldBuildLinksForEveryImportedDocument_InOneLookup()
    {
        UnitOfWork.Tasks
            .GetTaskViewModels(Arg.Any<IReadOnlyCollection<string>>(), WorkspaceKey, Arg.Any<CancellationToken>())
            .Returns([new TaskViewModel { Id = 12, SystemId = "NET-12", Name = "Spike" }]);

        await Send(
            new ImportPagesRequest(),
            File("Notes/a.md", "See [[task:NET-12]]."),
            File("Notes/b.md", "Also [[task:NET-12]] and [[task:NET-99]]."));

        var documentIds = AddedPages.Where(page => page.Kind == PageKind.Document).Select(page => page.Id).ToList();

        AddedLinks.Select(link => link.PageId).Should().Equal(documentIds);
        AddedLinks.Should().OnlyContain(link => link.TargetType == PageLinkTargetType.Task && link.TargetId == "12");
        await UnitOfWork.Tasks.Received(1).GetTaskViewModels(
            Arg.Any<IReadOnlyCollection<string>>(),
            WorkspaceKey,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldIndexAndLogEveryCreatedPageOnce_EvenWhenPathsCollide()
    {
        var result = await Send(new ImportPagesRequest(), File("x.md", "File"), File("x.md/child.md", "Child"));

        var pageIds = AddedPages.Select(page => page.Id).ToArray();
        var logCall = Activity.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IActivityLogger.LogMany));
        var configure = (Action<ActivityMultipleOptions>)logCall.GetArguments()[0]!;
        var logged = new ActivityMultipleOptions();

        configure(logged);

        result.Payload!.Created.Should().Be(3);
        pageIds.Should().HaveCount(3).And.OnlyHaveUniqueItems();
        await EventPublisher.Received(1).Dispatch(Arg.Any<SearchIndexEvent>());
        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Index, pageIds);
        logged.EntityIds.Should().Equal(pageIds);
        logged.EntityType.Should().Be(EntityType.Page);
        logged.Type.Should().Be(ActivityType.Create);
    }

    private void AssignIds(IEnumerable<Page> pages)
    {
        AddedPages = pages.ToList();

        for (var index = 0; index < AddedPages.Count; index++)
        {
            AddedPages[index].Id = FirstPageId + index;
        }
    }

    private static PageImportFile File(string path, string content)
    {
        return new PageImportFile(path, content);
    }

    private Task<ClientResponse<ImportPagesResultViewModel>> Send(ImportPagesRequest request, params PageImportFile[] files)
    {
        var upload = new PageImportRead { Files = [.. files] };

        return Handler.Handle(new ImportPagesCommand(request, upload), TestContext.Current.CancellationToken).AsTask();
    }
}
