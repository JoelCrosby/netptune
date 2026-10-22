using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Colors;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class PublicPagesEndpointTests
{
    private readonly NetptuneFixture Fixture;

    public PublicPagesEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task AnonymousTree_ShouldOnlyListPublishedDocuments_AndTheFoldersLeadingToThem()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var folder = await Create(owner, new CreatePageRequest { Kind = PageKind.Folder, Title = "Guides" });
        var published = await Create(owner, new CreatePageRequest { ParentId = folder.Id, Title = "Getting started" });
        var draft = await Create(owner, new CreatePageRequest { ParentId = folder.Id, Title = "Unfinished" });
        var hiddenChild = await Create(owner, new CreatePageRequest { ParentId = draft.Id, Title = "Inside a draft" });
        var emptyFolder = await Create(owner, new CreatePageRequest { Kind = PageKind.Folder, Title = "Empty" });

        await Publish(owner, published.Id);
        await Publish(owner, hiddenChild.Id);

        var tree = await GetPayload<List<PageTreeNodeViewModel>>(anonymous, "api/pages/tree");

        tree.Select(node => node.Id).Should().BeEquivalentTo([folder.Id, published.Id]);
        tree.Should().NotContain(node => node.Id == emptyFolder.Id);
    }

    [Fact]
    public async Task AnonymousGet_ShouldReturnPublishedPages_AndHideDrafts()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var published = await Create(owner, new CreatePageRequest { Title = "Public notes" });
        var draft = await Create(owner, new CreatePageRequest { Title = "Private notes" });

        await Publish(owner, published.Id);

        var publishedResponse = await anonymous.GetAsync($"api/pages/{published.Slug}", TestContext.Current.CancellationToken);
        var draftResponse = await anonymous.GetAsync($"api/pages/{draft.Slug}", TestContext.Current.CancellationToken);
        var draftLinksResponse = await anonymous.GetAsync($"api/pages/{draft.Id}/links", TestContext.Current.CancellationToken);

        publishedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        draftResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        draftLinksResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnonymousChildren_ShouldLeaveOutDrafts()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var folder = await Create(owner, new CreatePageRequest { Kind = PageKind.Folder, Title = "Handbook" });
        var published = await Create(owner, new CreatePageRequest { ParentId = folder.Id, Title = "Holidays" });

        await Create(owner, new CreatePageRequest { ParentId = folder.Id, Title = "Salaries" });
        await Publish(owner, published.Id);

        var children = await GetPayload<List<PageChildViewModel>>(anonymous, $"api/pages/{folder.Id}/children");

        children.Select(child => child.Id).Should().Equal(published.Id);
    }

    [Fact]
    public async Task AnonymousLinks_ShouldLeaveOutDrafts_OnAPublishedPage()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var page = await Create(owner, new CreatePageRequest { Title = "Hub" });
        var publishedTarget = await Create(owner, new CreatePageRequest { Title = "Published target" });
        var draftTarget = await Create(owner, new CreatePageRequest { Title = "Draft target" });
        var publishedSource = await Create(owner, new CreatePageRequest { Title = "Published source" });
        var draftSource = await Create(owner, new CreatePageRequest { Title = "Draft source" });

        await Save(owner, page, $"[[page:{publishedTarget.Id}|one]] [[page:{draftTarget.Id}|two]]");
        await Save(owner, publishedSource, $"[[page:{page.Id}|hub]]");
        await Save(owner, draftSource, $"[[page:{page.Id}|hub]]");
        await Publish(owner, page.Id);
        await Publish(owner, publishedTarget.Id);
        await Publish(owner, publishedSource.Id);

        var links = await GetPayload<PageLinksViewModel>(anonymous, $"api/pages/{page.Id}/links");

        links.Pages.Select(linked => linked.Id).Should().Equal(publishedTarget.Id);
        links.Backlinks.Select(backlink => backlink.PageId).Should().Equal(publishedSource.Id);
    }

    [Fact]
    public async Task AnonymousTaskPages_ShouldLeaveOutDrafts()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var task = await CreateTask(owner);
        var published = await Create(owner, new CreatePageRequest { Title = "Published mention" });
        var draft = await Create(owner, new CreatePageRequest { Title = "Draft mention" });

        await Save(owner, published, $"[[task:{task.SystemId}]]");
        await Save(owner, draft, $"[[task:{task.SystemId}]]");
        await Publish(owner, published.Id);

        var pages = await GetPayload<List<PageBacklinkViewModel>>(anonymous, $"api/pages/linked-to-task/{task.SystemId}");

        pages.Select(page => page.PageId).Should().Equal(published.Id);
    }

    [Theory]
    [InlineData("api/pages/recent")]
    [InlineData("api/pages/archived")]
    [InlineData("api/pages/needs-review")]
    public async Task AnonymousMemberLists_ShouldBeEmpty(string route)
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var page = await Create(owner, new CreatePageRequest { Title = "Listed" });

        await Publish(owner, page.Id);

        var pages = await GetPayload<List<object>>(anonymous, route);

        pages.Should().BeEmpty();
    }

    [Fact]
    public async Task AnonymousVersions_ShouldBeEmpty_ForAPublishedPage()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var page = await Create(owner, new CreatePageRequest { Title = "Versioned", Body = "First draft" });

        await Publish(owner, page.Id);

        var versions = await GetPayload<List<PageVersionSummaryViewModel>>(anonymous, $"api/pages/{page.Id}/versions");

        versions.Should().BeEmpty();
    }

    [Fact]
    public async Task AnonymousFavourite_ShouldBeDenied()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();

        var page = await Create(owner, new CreatePageRequest { Title = "Favourite" });

        await Publish(owner, page.Id);

        var response = await anonymous.PutAsync($"api/pages/{page.Id}/favourite", null, TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task AnonymousTree_ShouldBeDenied_WhenPagesAreNotShared()
    {
        var (owner, anonymous) = await CreatePublicWorkspace();
        var withoutPages = NetptunePermissions.PublicReadable
            .Where(permission => permission != NetptunePermissions.Pages.Read)
            .ToList();

        await UpdateWorkspace(owner, new UpdateWorkspaceRequest { Slug = WorkspaceSlug(owner), PublicPermissions = withoutPages });

        var response = await anonymous.GetAsync("api/pages/tree", TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task SignedInTree_ShouldStillListDrafts()
    {
        var (owner, _) = await CreatePublicWorkspace();

        var draft = await Create(owner, new CreatePageRequest { Title = "Members only" });

        var tree = await GetPayload<List<PageTreeNodeViewModel>>(owner, "api/pages/tree");

        tree.Should().Contain(node => node.Id == draft.Id);
    }

    private async Task<(HttpClient Owner, HttpClient Anonymous)> CreatePublicWorkspace()
    {
        var slug = $"public-{Guid.NewGuid():N}"[..20];
        var owner = Fixture.CreateNetptuneClient();

        var createResponse = await owner.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        }, TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        owner.DefaultRequestHeaders.Remove("workspace");
        owner.DefaultRequestHeaders.Add("workspace", slug);

        await UpdateWorkspace(owner, new UpdateWorkspaceRequest { Slug = slug, IsPublic = true });

        var anonymous = Fixture.CreateAnonymousNetptuneClient(slug);

        return (owner, anonymous);
    }

    private static string WorkspaceSlug(HttpClient client)
    {
        return client.DefaultRequestHeaders.GetValues("workspace").Single();
    }

    private static async Task UpdateWorkspace(HttpClient owner, UpdateWorkspaceRequest request)
    {
        var response = await owner.PutAsJsonAsync("api/workspaces", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<PageViewModel> Create(HttpClient owner, CreatePageRequest request)
    {
        var response = await owner.PostAsJsonAsync("api/pages", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private static async Task Save(HttpClient owner, PageViewModel page, string body)
    {
        var request = new SavePageRequest { Title = page.Title, Body = body, Revision = page.Revision };
        var response = await owner.PutAsJsonAsync($"api/pages/{page.Id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<TaskViewModel> CreateTask(HttpClient owner)
    {
        var slug = WorkspaceSlug(owner);

        using var scope = Fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var project = await context.Projects
            .FirstAsync(item => item.Workspace!.Slug == slug && !item.IsDeleted, TestContext.Current.CancellationToken);
        var request = new AddProjectTaskRequest { Name = "Mentioned task", ProjectId = project.Id };
        var response = await owner.PostAsJsonAsync("api/tasks", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }

    private static async Task Publish(HttpClient owner, int id)
    {
        var request = new UpdatePageRequest { Status = PageStatus.Published };
        var response = await owner.PatchAsJsonAsync($"api/pages/{id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<T> GetPayload<T>(HttpClient client, string route)
    {
        var response = await client.GetAsync(route, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<T>>(TestContext.Current.CancellationToken);

        return result.Payload!;
    }
}
