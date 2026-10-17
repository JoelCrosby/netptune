using System.Net;
using System.Net.Http.Json;
using System.Text;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SourceControlTaskLinkTests
{
    private const string WebhookUrl = "api/webhooks/source-control/gitlab";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;
    private readonly HttpClient Anonymous;

    public SourceControlTaskLinkTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
        Anonymous = fixture.CreateAnonymousNetptuneClient("netptune");
    }

    [Fact]
    public async Task PullRequest_ShouldLinkTheTaskItMentions()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();

        await ProcessPullRequest(account, repo.ExternalId, "7", $"Fix search ({task.SystemId.ToUpperInvariant()})", "Open");

        var links = await LinksFor(task.Id, repo.Id);
        var link = links.Should().ContainSingle().Subject;

        link.Kind.Should().Be(TaskCodeLinkKind.PullRequest);
        link.ExternalRef.Should().Be("7");
        link.PullRequestState.Should().Be(ScmPullRequestState.Open);
        link.AuthorLogin.Should().Be("tester");
    }

    [Fact]
    public async Task PullRequest_ShouldUpdateTheExistingLinkWhenItMerges()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();

        await ProcessPullRequest(account, repo.ExternalId, "8", $"Fix {task.SystemId}", "Open");
        await ProcessPullRequest(account, repo.ExternalId, "8", "Retitled without the id", "Merged");

        var link = (await LinksFor(task.Id, repo.Id)).Should().ContainSingle().Subject;

        link.PullRequestState.Should().Be(ScmPullRequestState.Merged);
        link.Title.Should().Be("Retitled without the id");
    }

    [Fact]
    public async Task PullRequest_ShouldNotLinkFromARepoThatIsNotLinked()
    {
        var (account, linkedRepo) = await ConnectWithLinkedRepo();
        var unlinkedRepo = (await GetRepos(linkedRepo.ConnectionId)).Single(repo => !repo.IsLinked);
        var task = await AnyTask();

        await ProcessPullRequest(account, unlinkedRepo.ExternalId, "9", $"Fix {task.SystemId}", "Open");

        (await LinksFor(task.Id, unlinkedRepo.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task PullRequest_ShouldIgnoreAnIdWithNoMatchingTask()
    {
        var (account, repo) = await ConnectWithLinkedRepo();
        var task = await AnyTask();
        var missingId = $"{task.SystemId.Split('-')[0]}-999999";

        await ProcessPullRequest(account, repo.ExternalId, "10", $"Fix {missingId}", "Open");

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var links = await context.TaskCodeLinks
            .Where(link => link.RepoId == repo.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        links.Should().BeEmpty();
    }

    private async Task<(string Account, RepoRef Repo)> ConnectWithLinkedRepo()
    {
        var account = $"link-{Guid.NewGuid():N}"[..20];

        var connect = await Client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            new CompleteSourceControlConnectRequest
            {
                Provider = SourceControlProvider.GitLab,
                Parameters = new Dictionary<string, string> { ["account"] = account },
            },
            TestContext.Current.CancellationToken);

        var connection = await connect.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(
            TestContext.Current.CancellationToken);

        var repos = await GetRepos(connection.Payload!.Id);
        var repo = repos.First();

        var linked = await Client.PutAsJsonAsync(
            $"api/source-control/repos/{repo.Id}/linked",
            new SetSourceControlRepoLinkedRequest { IsLinked = true },
            TestContext.Current.CancellationToken);

        linked.StatusCode.Should().Be(HttpStatusCode.OK);

        return (account, repo with { IsLinked = true });
    }

    private async Task<List<RepoRef>> GetRepos(int connectionId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var repos = await context.SourceControlRepos
            .AsNoTracking()
            .Where(repo => repo.ConnectionId == connectionId)
            .OrderBy(repo => repo.FullName)
            .Select(repo => new RepoRef(repo.Id, repo.ConnectionId, repo.ExternalId, repo.IsLinked))
            .ToListAsync(TestContext.Current.CancellationToken);

        return repos;
    }

    private async Task<(int Id, string SystemId)> AnyTask()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var task = await context.ProjectTasks
            .AsNoTracking()
            .Where(task => !task.IsDeleted && task.Workspace!.Slug == "netptune")
            .OrderBy(task => task.Id)
            .Select(task => new { task.Id, task.Project!.Key, task.ProjectScopeId })
            .FirstAsync(TestContext.Current.CancellationToken);

        return (task.Id, $"{task.Key}-{task.ProjectScopeId}");
    }

    private async Task ProcessPullRequest(string account, string repoExternalId, string number, string title, string state)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);

        var body = $$"""{ "account": "{{account}}", "repo": "{{repoExternalId}}", "number": "{{number}}", "title": "{{title}}", "state": "{{state}}" }""";

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Add(TestSourceControlProvider.DeliveryHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestSourceControlProvider.EventHeader, TestSourceControlProvider.PullRequestEvent);
        request.Headers.Add(TestSourceControlProvider.SignatureHeader, TestSourceControlProvider.ValidSignature);

        var response = await Anonymous.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IScmWebhookProcessor>();

        var pending = await context.ScmWebhookDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.ExternalAccountId == account && delivery.ProcessedAt == null)
            .Select(delivery => delivery.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        foreach (var deliveryId in pending)
        {
            await processor.Process(deliveryId, TestContext.Current.CancellationToken);
        }
    }

    private async Task<List<TaskCodeLink>> LinksFor(int taskId, int repoId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var links = await context.TaskCodeLinks
            .AsNoTracking()
            .Where(link => link.TaskId == taskId && link.RepoId == repoId)
            .ToListAsync(TestContext.Current.CancellationToken);

        return links;
    }

    private sealed record RepoRef(int Id, int ConnectionId, string ExternalId, bool IsLinked);
}
