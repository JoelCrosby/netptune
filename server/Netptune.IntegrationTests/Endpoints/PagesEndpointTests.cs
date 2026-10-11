using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Tags;
using Netptune.Entities.Contexts;
using Netptune.Handlers.Pages.Commands;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class PagesEndpointTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public PagesEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Create_ShouldAddAnUntitledDraft_ThatTheTreeAndSlugLookupReturn()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document });

        created.Title.Should().Be(PageTitle.Untitled);
        created.Status.Should().Be(PageStatus.Draft);
        created.Revision.Should().Be(1);
        created.Body.Should().BeEmpty();

        var tree = await GetTree();

        tree.Should().Contain(node => node.Id == created.Id && node.Slug == created.Slug);

        var fetched = await Client.GetFromJsonAsync<ClientResponse<PageViewModel>>(
            $"api/pages/{created.Slug}",
            TestContext.Current.CancellationToken);

        fetched.Payload!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Save_ShouldBumpTheRevision_WhenTheRevisionMatches()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Runbook" });

        var response = await Save(created.Id, new SavePageRequest { Title = "Deploy runbook", Body = "## Steps", Revision = created.Revision });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        result.Payload!.Revision.Should().Be(created.Revision + 1);
        result.Payload.Title.Should().Be("Deploy runbook");
        result.Payload.Body.Should().Be("## Steps");
    }

    [Fact]
    public async Task Save_ShouldMoveTheSlugToTheNewTitle_WhileTheOldSlugStillFindsThePage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document });
        var response = await Save(created.Id, new SavePageRequest { Title = "Incident review", Revision = created.Revision });
        var saved = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(cancellationToken);

        saved!.Payload!.Slug.Should().StartWith("incident-review-");
        PageSlug.IdOf(saved.Payload.Slug).Should().Be(PageSlug.IdOf(created.Slug));

        var fetched = await Client.GetFromJsonAsync<ClientResponse<PageViewModel>>($"api/pages/{created.Slug}", cancellationToken);

        fetched!.Payload!.Id.Should().Be(created.Id);
        fetched.Payload.Slug.Should().Be(saved.Payload.Slug);
    }

    [Fact]
    public async Task GetBySlug_ShouldNotFindAnArchivedPage_ByItsOldSlug()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document });

        await Save(created.Id, new SavePageRequest { Title = "Retired notes", Revision = created.Revision });
        await Client.DeleteAsync($"api/pages/{created.Id}", cancellationToken);

        var response = await Client.GetAsync($"api/pages/{created.Slug}", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_ShouldNotFindAPage_FromAnotherWorkspaceByItsOldSlug()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document });

        await Save(created.Id, new SavePageRequest { Title = "Private plans", Revision = created.Revision });

        using var otherWorkspaceClient = Fixture.CreateNetptuneClient();

        otherWorkspaceClient.DefaultRequestHeaders.Remove("workspace");
        otherWorkspaceClient.DefaultRequestHeaders.Add("workspace", "linux");

        var response = await otherWorkspaceClient.GetAsync($"api/pages/{created.Slug}", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Save_ShouldReturnConflictWithTheCurrentPage_WhenTheRevisionIsStale()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Shared notes" });

        await Save(created.Id, new SavePageRequest { Title = "Shared notes", Body = "first", Revision = created.Revision });

        var response = await Save(created.Id, new SavePageRequest { Title = "Shared notes", Body = "second", Revision = created.Revision });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        result.Message.Should().Be(SavePageCommandHandler.ConflictMessage);
        result.Payload!.Body.Should().Be("first");
        result.Payload.Revision.Should().Be(created.Revision + 1);
    }

    [Fact]
    public async Task Save_ShouldReject_WhenTheBodyIsOverTheLimit()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Huge" });
        var body = new string('a', PageBody.MaxBytes + 1);

        var response = await Save(created.Id, new SavePageRequest { Title = "Huge", Body = body, Revision = created.Revision });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        result.Message.Should().Be(PageBody.TooLargeMessage);
    }

    [Fact]
    public async Task Move_ShouldRejectMovingAFolderIntoItsOwnChild()
    {
        var folder = await Create(new CreatePageRequest { Kind = PageKind.Folder, Title = "Engineering" });
        var child = await Create(new CreatePageRequest { Kind = PageKind.Document, ParentId = folder.Id, Title = "RFCs" });

        var response = await Move(folder.Id, new MovePageRequest { ParentId = child.Id, SortOrder = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        result.Message.Should().Be("A page cannot be moved inside itself.");
    }

    [Fact]
    public async Task ArchiveAndRestore_ShouldTakeTheWholeSubtreeOutAndBackIn()
    {
        var folder = await Create(new CreatePageRequest { Kind = PageKind.Folder, Title = "Meeting notes" });
        var child = await Create(new CreatePageRequest { Kind = PageKind.Document, ParentId = folder.Id, Title = "Retro" });
        var grandchild = await Create(new CreatePageRequest { Kind = PageKind.Document, ParentId = child.Id, Title = "Actions" });

        var archiveResponse = await Client.DeleteAsync($"api/pages/{folder.Id}", TestContext.Current.CancellationToken);

        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var treeAfterArchive = await GetTree();
        var archivedIds = new[] { folder.Id, child.Id, grandchild.Id };

        treeAfterArchive.Should().NotContain(node => archivedIds.Contains(node.Id));

        var archived = await Client.GetFromJsonAsync<ClientResponse<List<ArchivedPageViewModel>>>(
            "api/pages/archived",
            TestContext.Current.CancellationToken);

        archived.Payload.Should().Contain(page => page.Id == folder.Id);
        archived.Payload.Should().NotContain(page => page.Id == child.Id);

        var restoreResponse = await Client.PostAsync($"api/pages/{folder.Id}/restore", null, TestContext.Current.CancellationToken);

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var treeAfterRestore = await GetTree();

        treeAfterRestore.Select(node => node.Id).Should().Contain(archivedIds);
    }

    [Fact]
    public async Task Restore_ShouldLeaveAChildArchivedOnItsOwnBeforeTheParentInTheArchive()
    {
        var folder = await Create(new CreatePageRequest { Kind = PageKind.Folder, Title = "Runbooks" });
        var keptChild = await Create(new CreatePageRequest { Kind = PageKind.Document, ParentId = folder.Id, Title = "Deploy" });
        var archivedChild = await Create(new CreatePageRequest { Kind = PageKind.Document, ParentId = folder.Id, Title = "Old rota" });

        await Client.DeleteAsync($"api/pages/{archivedChild.Id}", TestContext.Current.CancellationToken);
        await Client.DeleteAsync($"api/pages/{folder.Id}", TestContext.Current.CancellationToken);
        await Client.PostAsync($"api/pages/{folder.Id}/restore", null, TestContext.Current.CancellationToken);

        var tree = await GetTree();
        var treeIds = tree.Select(node => node.Id).ToList();

        treeIds.Should().Contain([folder.Id, keptChild.Id]);
        treeIds.Should().NotContain(archivedChild.Id);

        var archived = await Client.GetFromJsonAsync<ClientResponse<List<ArchivedPageViewModel>>>(
            "api/pages/archived",
            TestContext.Current.CancellationToken);

        archived.Payload.Should().Contain(page => page.Id == archivedChild.Id);
    }

    [Fact]
    public async Task Versions_ShouldFoldQuickSavesTogether_AndRestoreWritesANewVersion()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "History" });

        await Save(created.Id, new SavePageRequest { Title = "History", Body = "A", Revision = 1 });
        await Save(created.Id, new SavePageRequest { Title = "History", Body = "B", Revision = 2 });

        var versions = await GetVersions(created.Id);

        versions.Should().HaveCount(2);
        versions[0].Revision.Should().Be(3);
        versions[1].Revision.Should().Be(1);

        var latest = await Client.GetFromJsonAsync<ClientResponse<PageVersionViewModel>>(
            $"api/pages/{created.Id}/versions/{versions[0].Id}",
            TestContext.Current.CancellationToken);

        latest.Payload!.Body.Should().Be("B");
        latest.Payload.PreviousBody.Should().BeEmpty();

        var restoreResponse = await Client.PostAsync(
            $"api/pages/{created.Id}/versions/{versions[1].Id}/restore",
            null,
            TestContext.Current.CancellationToken);

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var restored = await restoreResponse.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        restored.Payload!.Body.Should().BeEmpty();
        restored.Payload.Revision.Should().Be(4);

        var afterRestore = await GetVersions(created.Id);

        afterRestore.Should().HaveCount(3);
        afterRestore[0].RestoredFromVersionId.Should().Be(versions[1].Id);
    }

    [Fact]
    public async Task Update_ShouldSetStatusAndReplaceTags()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Tagged" });
        var tags = await Client.GetFromJsonAsync<List<TagViewModel>>("api/tags/workspace", TestContext.Current.CancellationToken);
        var firstTwo = tags!.Take(2).Select(tag => tag.Id).ToList();

        var tagged = await Update(created.Id, new UpdatePageRequest { Status = PageStatus.Published, TagIds = firstTwo });

        tagged.Status.Should().Be(PageStatus.Published);
        tagged.Tags.Select(tag => tag.Id).Should().BeEquivalentTo(firstTwo);

        var retagged = await Update(created.Id, new UpdatePageRequest { TagIds = [firstTwo[1]] });

        retagged.Status.Should().Be(PageStatus.Published);
        retagged.Tags.Select(tag => tag.Id).Should().Equal(firstTwo[1]);
    }

    [Fact]
    public async Task Update_ShouldIgnoreTagIdsThatAreNotLiveInTheWorkspace()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Bad tag" });
        var tags = await Client.GetFromJsonAsync<List<TagViewModel>>("api/tags/workspace", TestContext.Current.CancellationToken);
        var realTagId = tags!.First().Id;

        var updated = await Update(created.Id, new UpdatePageRequest { TagIds = [int.MaxValue, realTagId] });

        updated.Tags.Select(tag => tag.Id).Should().Equal(realTagId);
    }

    [Fact]
    public async Task Save_ShouldRecordMentionsAsLinks_AndExposeBacklinks()
    {
        var task = await CreateTask();
        var target = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Linked target" });
        var source = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Linking source" });
        var body = $"Tracks [[task:{task.SystemId}]] and see [[page:{target.Id}|the target]].\n\n`[[task:{task.SystemId}]]` in code is ignored. [[task:nope-999]] is dropped.";

        await Save(source.Id, new SavePageRequest { Title = source.Title, Body = body, Revision = source.Revision });

        var links = await Client.GetFromJsonAsync<ClientResponse<PageLinksViewModel>>(
            $"api/pages/{source.Id}/links",
            TestContext.Current.CancellationToken);

        links.Payload!.Tasks.Should().ContainSingle(linked => linked.Id == task.Id);
        links.Payload.Pages.Should().ContainSingle(linked => linked.Id == target.Id);

        var targetLinks = await Client.GetFromJsonAsync<ClientResponse<PageLinksViewModel>>(
            $"api/pages/{target.Id}/links",
            TestContext.Current.CancellationToken);

        targetLinks.Payload!.Backlinks.Should().ContainSingle(backlink => backlink.PageId == source.Id)
            .Which.Snippet.Should().Contain("the target");

        var taskPages = await Client.GetFromJsonAsync<ClientResponse<List<PageBacklinkViewModel>>>(
            $"api/pages/linked-to-task/{task.SystemId}",
            TestContext.Current.CancellationToken);

        taskPages.Payload.Should().Contain(backlink => backlink.PageId == source.Id);

        await Save(source.Id, new SavePageRequest { Title = source.Title, Body = "No mentions now.", Revision = source.Revision + 1 });

        var cleared = await Client.GetFromJsonAsync<ClientResponse<PageLinksViewModel>>(
            $"api/pages/{source.Id}/links",
            TestContext.Current.CancellationToken);

        cleared.Payload!.Tasks.Should().BeEmpty();
        cleared.Payload.Pages.Should().BeEmpty();
    }

    [Fact]
    public async Task Favourite_ShouldShowInTheTree_AndClearWhenRemoved()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Onboarding" });

        var favouriteResponse = await Client.PutAsync($"api/pages/{created.Id}/favourite", null, TestContext.Current.CancellationToken);

        favouriteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var favouritedTree = await GetTree();

        favouritedTree.Single(node => node.Id == created.Id).IsFavourite.Should().BeTrue();

        await Client.DeleteAsync($"api/pages/{created.Id}/favourite", TestContext.Current.CancellationToken);

        var clearedTree = await GetTree();

        clearedTree.Single(node => node.Id == created.Id).IsFavourite.Should().BeFalse();
    }

    [Fact]
    public async Task Visit_ShouldSucceedRepeatedly_ForTheSamePage()
    {
        var created = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Personas" });

        var first = await Client.PostAsync($"api/pages/{created.Id}/visits", null, TestContext.Current.CancellationToken);
        var second = await Client.PostAsync($"api/pages/{created.Id}/visits", null, TestContext.Current.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ShouldPutAPageInAProjectSpace_AndItsChildrenWithIt()
    {
        var space = await Create(new CreatePageRequest { Kind = PageKind.Folder, Title = "Mobile docs", ProjectId = 1 });
        var child = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Release checklist", ParentId = space.Id });

        var tree = await GetTree();

        tree.Single(node => node.Id == space.Id).ProjectId.Should().Be(1);
        tree.Single(node => node.Id == child.Id).ProjectId.Should().Be(1);
    }

    [Fact]
    public async Task Import_ShouldCreateTheFolderTreeOfAZip_AsPagesInOneGo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = $"Handbook {Guid.NewGuid():N}"[..17];
        var zip = new MemoryStream();

        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, $"{folder}/on-call.md", "# On-call\n\nPage the owner.");
            WriteEntry(archive, $"{folder}/Runbooks/deploy.md", "Steps to deploy.");
            WriteEntry(archive, $"{folder}/diagram.png", "png");
        }

        zip.Position = 0;

        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(zip);

        content.Add(file, "files", "handbook.zip");

        var response = await Client.PostAsync("api/pages/import?projectId=1", content, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ImportPagesResultViewModel>>(cancellationToken);
        var tree = await GetTree();
        var root = tree.Single(node => node.Title == folder);
        var onCall = tree.Single(node => node.ParentId == root.Id && node.Title == "On-call");
        var runbooks = tree.Single(node => node.ParentId == root.Id && node.Title == "Runbooks");
        var deploy = tree.Single(node => node.ParentId == runbooks.Id && node.Title == "deploy");

        result!.Payload!.Created.Should().Be(4);
        result.Payload.Skipped.Should().ContainSingle(skipped => skipped.Contains("diagram.png"));
        result.Payload.FirstSlug.Should().Be(root.Slug);
        root.ProjectId.Should().Be(1);
        deploy.ProjectId.Should().Be(1);

        var page = await Client.GetFromJsonAsync<ClientResponse<PageViewModel>>($"api/pages/{onCall.Slug}", cancellationToken);

        page!.Payload!.Body.Should().Be("Page the owner.");
    }

    private static void WriteEntry(ZipArchive archive, string path, string text)
    {
        var entry = archive.CreateEntry(path);

        using var writer = new StreamWriter(entry.Open());

        writer.Write(text);
    }

    [Fact]
    public async Task Create_ShouldStartFromATemplate_WithItsTitleAndBody()
    {
        var page = await Create(new CreatePageRequest { Kind = PageKind.Document, TemplateKey = "rfc" });

        page.Title.Should().Be("RFC");
        page.Body.Should().Contain("## Problem").And.Contain("## Open questions");
    }

    [Fact]
    public async Task Create_ShouldRefuse_AnUnknownTemplate()
    {
        var request = new CreatePageRequest { Kind = PageKind.Document, TemplateKey = "nope" };
        var response = await Client.PostAsJsonAsync("api/pages", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldWriteReleaseNotes_FromASprintsCompletedTasks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var done = await CreateTask();
        var open = await CreateTask();
        var sprintRequest = new AddSprintRequest
        {
            Name = $"Release {Guid.NewGuid():N}"[..20],
            StartDate = DateTime.UtcNow.Date.AddYears(6),
            EndDate = DateTime.UtcNow.Date.AddYears(6).AddDays(14),
            ProjectId = 1,
        };
        var sprintResponse = await Client.PostAsJsonAsync("api/sprints", sprintRequest, cancellationToken);
        var sprintResult = await sprintResponse.Content.ReadFromJsonAsync<ClientResponse<SprintViewModel>>(cancellationToken);
        var sprint = sprintResult!.Payload!;

        using (var scope = Fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var tasks = await context.ProjectTasks.Where(task => task.Id == done.Id || task.Id == open.Id).ToListAsync(cancellationToken);
            var doneStatus = await context.Statuses.FirstAsync(
                status => status.WorkspaceId == tasks[0].WorkspaceId
                    && status.EntityType == EntityType.Task
                    && status.Category == StatusCategory.Done
                    && !status.IsDeleted,
                cancellationToken);

            foreach (var task in tasks)
            {
                task.SprintId = sprint.Id;
            }

            tasks.Single(task => task.Id == done.Id).StatusId = doneStatus.Id;

            await context.SaveChangesAsync(cancellationToken);
        }

        var notes = await Create(new CreatePageRequest
        {
            Kind = PageKind.Document,
            TemplateKey = PageTemplates.FromSprint,
            SprintId = sprint.Id,
        });

        notes.Title.Should().Be($"Release notes: {sprint.Name}");
        notes.Body.Should().Contain($"[[task:{done.SystemId}|");
        notes.Body.Should().NotContain(open.SystemId);
    }

    [Fact]
    public async Task Recent_ShouldListThePagesTheCallerVisited_LatestFirst()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Visited first" });
        var second = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Visited second" });

        await Client.PostAsync($"api/pages/{first.Id}/visits", null, cancellationToken);
        await Task.Delay(20, cancellationToken);
        await Client.PostAsync($"api/pages/{second.Id}/visits", null, cancellationToken);

        var recent = await Client.GetFromJsonAsync<ClientResponse<List<RecentPageViewModel>>>("api/pages/recent", cancellationToken);
        var ids = recent!.Payload!.Select(page => page.Id).ToList();

        ids.IndexOf(second.Id).Should().BeLessThan(ids.IndexOf(first.Id));
    }

    [Fact]
    public async Task NeedsReview_ShouldFlagStaleAndBrokenPages_UntilTheyAreMarkedReviewed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var task = await CreateTask();
        var stale = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Old runbook" });
        var broken = await Create(new CreatePageRequest { Kind = PageKind.Document, Title = "Linking page" });

        await Save(broken.Id, new SavePageRequest { Title = broken.Title, Body = $"Tracks [[task:{task.SystemId}]]", Revision = broken.Revision });
        await Update(stale.Id, new UpdatePageRequest { Status = PageStatus.Published });
        await Update(broken.Id, new UpdatePageRequest { Status = PageStatus.Published });

        using (var scope = Fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var old = DateTime.UtcNow.AddYears(-2);
            var stalePage = await context.Pages.SingleAsync(page => page.Id == stale.Id, cancellationToken);
            var deletedTask = await context.ProjectTasks.SingleAsync(item => item.Id == task.Id, cancellationToken);

            stalePage.CreatedAt = old;
            stalePage.UpdatedAt = old;
            deletedTask.IsDeleted = true;

            await context.SaveChangesAsync(cancellationToken);
            await context.Pages
                .Where(page => page.Id == stale.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(page => page.UpdatedAt, old), cancellationToken);
        }

        var flagged = await GetNeedingReview();

        flagged.Should().Contain(page => page.Id == broken.Id && page.Reason == PageReviewReason.BrokenLinks);
        flagged.Should().Contain(page => page.Id == stale.Id && page.Reason == PageReviewReason.Stale);

        var reviewed = await Client.PostAsync($"api/pages/{stale.Id}/reviewed", null, cancellationToken);

        reviewed.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterReview = await GetNeedingReview();

        afterReview.Should().NotContain(page => page.Id == stale.Id);

        using var verifyScope = Fixture.CreateScope();

        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<DataContext>();
        var reviewedPage = await verifyContext.Pages.AsNoTracking().SingleAsync(page => page.Id == stale.Id, cancellationToken);

        reviewedPage.UpdatedAt.Should().BeBefore(DateTime.UtcNow.AddYears(-1));
        reviewedPage.ReviewedAt.Should().NotBeNull();
    }

    private async Task<List<PageNeedingReviewViewModel>> GetNeedingReview()
    {
        var result = await Client.GetFromJsonAsync<ClientResponse<List<PageNeedingReviewViewModel>>>(
            "api/pages/needs-review",
            TestContext.Current.CancellationToken);

        return result!.Payload!;
    }

    // Its own task, because other test classes delete and move tasks while this one runs.
    private async Task<TaskViewModel> CreateTask()
    {
        var request = new AddProjectTaskRequest
        {
            Name = $"Mentioned task {Guid.NewGuid():N}",
            ProjectId = 1,
        };

        var response = await Client.PostAsJsonAsync("api/tasks", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>(TestContext.Current.CancellationToken);

        return result!.Payload!;
    }

    private async Task<PageViewModel> Create(CreatePageRequest request)
    {
        var response = await Client.PostAsJsonAsync("api/pages", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private Task<HttpResponseMessage> Save(int id, SavePageRequest request)
    {
        return Client.PutAsJsonAsync($"api/pages/{id}", request, TestContext.Current.CancellationToken);
    }

    private async Task<List<PageVersionSummaryViewModel>> GetVersions(int id)
    {
        var result = await Client.GetFromJsonAsync<ClientResponse<List<PageVersionSummaryViewModel>>>(
            $"api/pages/{id}/versions",
            TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private async Task<PageViewModel> Update(int id, UpdatePageRequest request)
    {
        var response = await Client.PatchAsJsonAsync($"api/pages/{id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private Task<HttpResponseMessage> Move(int id, MovePageRequest request)
    {
        return Client.PatchAsJsonAsync($"api/pages/{id}/move", request, TestContext.Current.CancellationToken);
    }

    private async Task<List<PageTreeNodeViewModel>> GetTree()
    {
        var result = await Client.GetFromJsonAsync<ClientResponse<List<PageTreeNodeViewModel>>>(
            "api/pages/tree",
            TestContext.Current.CancellationToken);

        return result.Payload!;
    }
}
