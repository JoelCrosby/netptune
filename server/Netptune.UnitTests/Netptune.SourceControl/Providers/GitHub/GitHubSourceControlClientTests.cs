using System.Net;

using FluentAssertions;

using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Registry;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.GitHub;

public sealed class GitHubSourceControlClientTests : IDisposable
{
    private const string BaseSha = "1111111111111111111111111111111111111111";
    private const string OtherSha = "9999999999999999999999999999999999999999";
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
    public async Task CreateBranch_ShouldCreateTheRefFromTheBaseBranchHead()
    {
        StubBranch("main", BaseSha);
        Harness.Http.On(HttpMethod.Post, "/repos/acme/netptune/git/refs", HttpStatusCode.Created, $$"""{ "ref": "refs/heads/feature/netp-1", "object": { "sha": "{{BaseSha}}" } }""");

        var result = await CreateBranch("feature/netp-1");

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.HeadSha.Should().Be(BaseSha);
        Harness.Http.Requests[^1].Body.Should().Be($$"""{"ref":"refs/heads/feature/netp-1","sha":"{{BaseSha}}"}""");
    }

    [Fact]
    public async Task CreateBranch_ShouldFailWhenTheBaseBranchDoesNotExist()
    {
        Harness.Http.On(HttpMethod.Get, $"{BranchesPath}/main", HttpStatusCode.NotFound, """{ "message": "Branch not found" }""");

        var result = await CreateBranch("feature/netp-1");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("base branch main");
        Harness.Http.Requests.Should().NotContain(request => IsCreateRef(request));
    }

    [Fact]
    public async Task CreateBranch_ShouldFailWhenADifferentBranchAlreadyHasThatName()
    {
        StubBranch("main", BaseSha);
        StubRejectedCreate();
        StubBranch("feature/netp-1", OtherSha);

        var result = await CreateBranch("feature/netp-1");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateBranch_ShouldSucceedWhenARetriedCreateFindsTheBranchAtTheBaseCommit()
    {
        StubBranch("main", BaseSha);
        StubRejectedCreate();
        StubBranch("feature/netp-1", BaseSha);

        var result = await CreateBranch("feature/netp-1");

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be("feature/netp-1");
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

    private async Task<ClientResponse<ScmBranch>> CreateBranch(string name)
    {
        var request = new ScmCreateBranch(name, "main");

        var result = await Harness.Client.CreateBranch(
            Context,
            Repository,
            request,
            TestContext.Current.CancellationToken);

        return result;
    }

    private void StubBranch(string name, string sha)
    {
        Harness.Http.On(HttpMethod.Get, $"{BranchesPath}/{name}", HttpStatusCode.OK, $$"""{ "name": "{{name}}", "commit": { "sha": "{{sha}}" } }""");
    }

    private void StubRejectedCreate()
    {
        Harness.Http.On(HttpMethod.Post, "/repos/acme/netptune/git/refs", HttpStatusCode.UnprocessableEntity, """{ "message": "Reference already exists" }""");
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

    private static bool IsCreateRef(RecordedRequest request)
    {
        var isPost = request.Method == HttpMethod.Post;
        var isRefsPath = request.Uri.AbsolutePath.EndsWith("/git/refs");

        return isPost && isRefsPath;
    }
}
