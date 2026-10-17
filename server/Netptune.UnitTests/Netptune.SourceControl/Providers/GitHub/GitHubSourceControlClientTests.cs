using System.Net;

using FluentAssertions;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Registry;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.GitHub;

public sealed class GitHubSourceControlClientTests : IDisposable
{
    private const string BaseSha = "1111111111111111111111111111111111111111";
    private const string BranchesPath = "/repos/acme/netptune/branches";

    private static readonly ScmRepositoryRef Repository = new("4242", "acme", "netptune");

    private readonly GitHubTestHarness Harness = new();
    private readonly ScmConnectionContext Context;

    public GitHubSourceControlClientTests()
    {
        var registry = new SourceControlProviderRegistry([Harness.Provider]);

        Context = registry.CreateContext(GitHubTestHarness.CreateConnection());
        Harness.StubInstallationToken();
    }

    [Fact]
    public async Task ListRepositories_ShouldFollowTheNextPageLink()
    {
        var firstPage = RepositoriesPage(1, "netptune");

        Harness.Http.On(HttpMethod.Get, "/installation/repositories?per_page=100", HttpStatusCode.OK, firstPage, response =>
        {
            response.Headers.Add(
                "Link",
                "<https://api.github.com/installation/repositories?per_page=100&page=2>; rel=\"next\"");
        });
        Harness.Http.On(
            HttpMethod.Get,
            "/installation/repositories?per_page=100&page=2",
            HttpStatusCode.OK,
            RepositoriesPage(2, "website"));

        var repositories = await ListRepositories();

        repositories.Select(repository => repository.FullName).Should().Equal("acme/netptune", "acme/website");
        repositories[0].IsPrivate.Should().BeTrue();
        repositories[0].DefaultBranch.Should().Be("main");
    }

    [Fact]
    public async Task ListRepositories_ShouldNotFollowANextPageLinkToAnotherHost()
    {
        var firstPage = RepositoriesPage(1, "netptune");

        Harness.Http.On(HttpMethod.Get, "/installation/repositories?per_page=100", HttpStatusCode.OK, firstPage, response =>
        {
            response.Headers.Add("Link", "<https://evil.example/steal?page=2>; rel=\"next\"");
        });

        var repositories = await ListRepositories();

        repositories.Should().ContainSingle();
        Harness.Http.Requests.Should().NotContain(request => request.Uri.Host == "evil.example");
    }

    [Fact]
    public async Task Requests_ShouldAuthenticateWithTheInstallationToken()
    {
        Harness.Http.On(
            HttpMethod.Get,
            "/installation/repositories?per_page=100",
            HttpStatusCode.OK,
            RepositoriesPage(1, "netptune"));

        await ListRepositories();

        Harness.Http.Requests[^1].Authorization.Should().Be("Bearer ghs_installation");
    }

    [Fact]
    public async Task GetBranch_ShouldReturnNullForABranchThatDoesNotExist()
    {
        Harness.Http.On(HttpMethod.Get, $"{BranchesPath}/feature/missing", HttpStatusCode.NotFound, """{ "message": "Branch not found" }""");

        var branch = await Harness.Client.GetBranch(
            Context,
            Repository,
            "feature/missing",
            TestContext.Current.CancellationToken);

        branch.Should().BeNull();
    }

    [Fact]
    public async Task GetBranch_ShouldLinkToTheBranchOnGitHub()
    {
        StubBranch("feature/netp-564-search", BaseSha);

        var branch = await Harness.Client.GetBranch(
            Context,
            Repository,
            "feature/netp-564-search",
            TestContext.Current.CancellationToken);

        var expected = new ScmBranch(
            "feature/netp-564-search",
            BaseSha,
            "https://github.com/acme/netptune/tree/feature/netp-564-search");

        branch.Should().Be(expected);
    }

    [Fact]
    public async Task Requests_ShouldSurfaceAnExhaustedRateLimitWithItsResetTime()
    {
        var reset = Harness.Clock.GetUtcNow().AddMinutes(20);

        Harness.Http.On(HttpMethod.Get, "/installation/repositories?per_page=100", HttpStatusCode.Forbidden, """{ "message": "API rate limit exceeded" }""", response =>
        {
            response.Headers.Add("x-ratelimit-remaining", "0");
            response.Headers.Add("x-ratelimit-reset", reset.ToUnixTimeSeconds().ToString());
        });

        var list = async () => await ListRepositories();

        var thrown = await list.Should().ThrowAsync<ScmRateLimitedException>();
        thrown.Which.RetryAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(reset.ToUnixTimeSeconds()));
    }

    [Fact]
    public async Task Requests_ShouldSurfaceAnUnexpectedFailureAsAProviderException()
    {
        Harness.Http.On(HttpMethod.Get, "/installation/repositories?per_page=100", HttpStatusCode.InternalServerError, """{ "message": "boom" }""");

        var list = async () => await ListRepositories();

        var thrown = await list.Should().ThrowAsync<ScmProviderException>();
        thrown.Which.StatusCode.Should().Be(500);
    }

    public void Dispose()
    {
        Harness.Dispose();
    }

    private async Task<List<ScmRepository>> ListRepositories()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repositories = Harness.Client.ListRepositories(Context, cancellationToken);

        var listed = await repositories.ToListAsync(cancellationToken);

        return listed;
    }

    private void StubBranch(string name, string sha)
    {
        Harness.Http.On(HttpMethod.Get, $"{BranchesPath}/{name}", HttpStatusCode.OK, $$"""{ "name": "{{name}}", "commit": { "sha": "{{sha}}" } }""");
    }

    private static string RepositoriesPage(long id, string name)
    {
        return $$"""
            {
              "total_count": 2,
              "repositories": [
                {
                  "id": {{id}},
                  "name": "{{name}}",
                  "full_name": "acme/{{name}}",
                  "private": true,
                  "archived": false,
                  "html_url": "https://github.com/acme/{{name}}",
                  "default_branch": "main",
                  "owner": { "id": 11, "login": "acme", "type": "Organization" }
                }
              ]
            }
            """;
    }
}
