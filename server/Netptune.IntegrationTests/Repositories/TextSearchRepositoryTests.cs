using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.Pages;
using Netptune.Core.ViewModels.ProjectTasks;

using Xunit;

namespace Netptune.IntegrationTests.Repositories;

public sealed class TextSearchRepositoryTests
{
    private const string WorkspaceKey = "netptune";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;
    private readonly string Token = $"tok{Guid.NewGuid():N}"[..16];

    public TextSearchRepositoryTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task FindPages_ShouldMatchALiteral_IgnoringCase_AndPage()
    {
        await CreatePage("A", $"First line\nmentions {Token.ToUpperInvariant()} here");
        await CreatePage("B", $"Also {Token}");
        await CreatePage("C", "Nothing to see");

        var firstPage = await FindPages(new TextPattern(Token, false, false), page: 1, pageSize: 1);
        var secondPage = await FindPages(new TextPattern(Token, false, false), page: 2, pageSize: 1);

        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Should().ContainSingle();
        secondPage.Items.Should().ContainSingle();
        secondPage.Items[0].Id.Should().NotBe(firstPage.Items[0].Id);
    }

    [Fact]
    public async Task FindPages_ShouldMatchCase_WhenAskedTo()
    {
        await CreatePage("A", $"mentions {Token.ToUpperInvariant()}");

        var found = await FindPages(new TextPattern(Token, false, true));

        found.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task FindPages_ShouldTreatLikeWildcardsAsLiteralText()
    {
        await CreatePage("A", $"{Token} 100% done");
        await CreatePage("B", $"{Token} 100 is done");

        var found = await FindPages(new TextPattern($"{Token} 100%", false, false));

        found.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task FindPages_ShouldAnchorARegexAtLineBreaks()
    {
        await CreatePage("A", $"Intro\n{Token} starts this line");
        await CreatePage("B", $"Intro mentions {Token} mid-line");

        var found = await FindPages(new TextPattern($"^{Token}", true, false));

        found.TotalCount.Should().Be(1);
        found.Items[0].Body.Should().Contain("starts this line");
    }

    [Fact]
    public async Task FindPages_ShouldOnlySearchTheGivenScope()
    {
        var inside = await CreatePage("A", Token);
        await CreatePage("B", Token);

        var found = await FindPages(new TextPattern(Token, false, false), scopeIds: [inside.Id]);

        found.Items.Select(page => page.Id).Should().Equal(inside.Id);
    }

    [Fact]
    public async Task FindPages_ShouldRefuse_ARegexPostgresCannotRead()
    {
        var find = () => FindPages(new TextPattern("(?<name>x)", true, false));

        await find.Should().ThrowAsync<InvalidTextPatternException>();
    }

    [Fact]
    public async Task FindTasks_ShouldMatchNamesDescriptionsAndComments_AsAsked()
    {
        var named = await CreateTask($"Named {Token}", "Plain description");
        var described = await CreateTask("Described", $"Line one\n{Token} in the description");
        var commented = await CreateTask("Commented", "Plain description");

        await CreateComment(commented.SystemId, $"A comment about {Token}");

        var byName = await FindTasks(new TaskTextSearch { Pattern = new TextPattern(Token, false, false), InName = true });
        var byDescription = await FindTasks(new TaskTextSearch { Pattern = new TextPattern(Token, false, false), InDescription = true });
        var byComment = await FindTasks(new TaskTextSearch { Pattern = new TextPattern(Token, false, false), InComments = true });
        var byAll = await FindTasks(new TaskTextSearch
        {
            Pattern = new TextPattern(Token, false, false),
            InName = true,
            InDescription = true,
            InComments = true,
        });

        byName.Items.Select(task => task.Id).Should().Equal(named.Id);
        byDescription.Items.Select(task => task.Id).Should().Equal(described.Id);
        byComment.Items.Select(task => task.Id).Should().Equal(commented.Id);
        byAll.Items.Select(task => task.Id).Should().BeEquivalentTo([named.Id, described.Id, commented.Id]);
        byAll.Items.Single(task => task.Id == described.Id).Description.Should().Contain(Token);
    }

    [Fact]
    public async Task FindTaskComments_ShouldCountEveryMatch_ButOnlyReadUpToTheLimit()
    {
        var task = await CreateTask("Commented", "Plain description");

        await CreateComment(task.SystemId, $"{Token} one");
        await CreateComment(task.SystemId, $"{Token} two");
        await CreateComment(task.SystemId, "unrelated");

        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var workspaceId = await GetWorkspaceId(unitOfWork);
        var matches = await unitOfWork.Comments.FindTaskCommentsByText(
            workspaceId,
            [task.Id],
            new TextPattern(Token, false, false),
            take: 1,
            TestContext.Current.CancellationToken);

        matches.CountsByTask[task.Id].Should().Be(2);
        matches.Comments.Should().ContainSingle().Which.Body.Should().Contain("one");
    }

    [Fact]
    public async Task GetTaskTexts_ShouldReadOnePageOfTasks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var workspaceId = await GetWorkspaceId(unitOfWork);
        var page = new PageRequest { Page = 1, PageSize = 2 };

        var texts = await unitOfWork.Tasks.GetTexts(workspaceId, projectId: null, page, TestContext.Current.CancellationToken);

        texts.Items.Should().HaveCount(2);
        texts.TotalCount.Should().BeGreaterThan(2);
    }

    private async Task<PagedResponse<PageBodyViewModel>> FindPages(
        TextPattern pattern,
        int page = 1,
        int pageSize = 25,
        IReadOnlyCollection<int>? scopeIds = null)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var workspaceId = await GetWorkspaceId(unitOfWork);
        var search = new PageTextSearch { Pattern = pattern, ScopeIds = scopeIds, Page = page, PageSize = pageSize };

        return await unitOfWork.Pages.FindByText(workspaceId, search, TestContext.Current.CancellationToken);
    }

    private async Task<PagedResponse<TaskTextViewModel>> FindTasks(TaskTextSearch search)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var workspaceId = await GetWorkspaceId(unitOfWork);

        return await unitOfWork.Tasks.FindByText(workspaceId, search, TestContext.Current.CancellationToken);
    }

    private static async Task<int> GetWorkspaceId(INetptuneUnitOfWork unitOfWork)
    {
        var workspaceId = await unitOfWork.Workspaces.GetIdBySlug(WorkspaceKey, TestContext.Current.CancellationToken);

        return workspaceId!.Value;
    }

    private async Task<PageViewModel> CreatePage(string title, string body)
    {
        var request = new CreatePageRequest { Title = $"{title} {Guid.NewGuid():N}", Body = body };
        var response = await Client.PostAsJsonAsync("api/pages", request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PageViewModel>>(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task<TaskViewModel> CreateTask(string name, string description)
    {
        var request = new AddProjectTaskRequest { Name = name, Description = description, ProjectId = 1 };
        var response = await Client.PostAsJsonAsync("api/tasks", request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task CreateComment(string systemId, string body)
    {
        var request = new AddCommentRequest { Comment = body, SystemId = systemId };
        var response = await Client.PostAsJsonAsync("api/comments/task", request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}
