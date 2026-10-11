using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Pages.Commands;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class PagesEndpointTests
{
    private readonly HttpClient Client;

    public PagesEndpointTests(NetptuneFixture fixture)
    {
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
