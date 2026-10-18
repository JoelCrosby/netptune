using System.Net;
using System.Text;

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
    public async Task GetTree_ShouldListFilesAndDirectoriesButNotSubmodules()
    {
        Harness.Http.On(HttpMethod.Get, $"/repos/acme/netptune/git/trees/{BaseSha}?recursive=1", HttpStatusCode.OK, $$"""
            {
              "sha": "{{BaseSha}}",
              "truncated": true,
              "tree": [
                { "path": "src", "type": "tree" },
                { "path": "src/app.ts", "type": "blob", "size": 42 },
                { "path": "vendor/lib", "type": "commit" }
              ]
            }
            """);

        var tree = await Harness.Client.GetTree(Context, Repository, BaseSha, TestContext.Current.CancellationToken);

        tree.IsTruncated.Should().BeTrue();
        tree.Entries.Should().Equal(new ScmTreeEntry("src", true, null), new ScmTreeEntry("src/app.ts", false, 42));
    }

    [Fact]
    public async Task GetFile_ShouldDecodeTheContent()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("line one\nline two\n"));

        StubFile("README.md", $$"""{ "type": "file", "path": "README.md", "size": 18, "encoding": "base64", "content": "{{encoded}}", "html_url": "https://github.com/acme/netptune/blob/main/README.md" }""");

        var token = TestContext.Current.CancellationToken;
        var file = await Harness.Client.GetFile(Context, Repository, "README.md", BaseSha, token);

        file!.Text.Should().Be("line one\nline two\n");
        file.IsBinary.Should().BeFalse();
        file.WebUrl.Should().EndWith("README.md");
    }

    [Fact]
    public async Task GetFile_ShouldFlagABinaryFile()
    {
        var encoded = Convert.ToBase64String([0x89, 0x50, 0x00, 0x47]);

        StubFile("logo.png", $$"""{ "type": "file", "path": "logo.png", "size": 4, "encoding": "base64", "content": "{{encoded}}", "html_url": "https://github.com/x" }""");

        var file = await Harness.Client.GetFile(Context, Repository, "logo.png", BaseSha, TestContext.Current.CancellationToken);

        file!.IsBinary.Should().BeTrue();
        file.Text.Should().BeNull();
    }

    [Fact]
    public async Task GetFile_ShouldFlagAFileTooLargeToBeInlined()
    {
        StubFile("data.json", """{ "type": "file", "path": "data.json", "size": 5000000, "encoding": "none", "content": "", "html_url": "https://github.com/x" }""");

        var file = await Harness.Client.GetFile(Context, Repository, "data.json", BaseSha, TestContext.Current.CancellationToken);

        file!.IsTooLarge.Should().BeTrue();
    }

    [Fact]
    public async Task GetFile_ShouldReturnNothingForADirectory()
    {
        StubFile("src", """[ { "type": "file", "path": "src/app.ts" } ]""");

        var file = await Harness.Client.GetFile(Context, Repository, "src", BaseSha, TestContext.Current.CancellationToken);

        file.Should().BeNull();
    }

    [Fact]
    public async Task SearchCode_ShouldScopeTheQueryToTheRepositoryAndAskForExcerpts()
    {
        var query = Uri.EscapeDataString("UseCase repo:acme/netptune");

        Harness.Http.On(HttpMethod.Get, $"/search/code?q={query}&per_page=20", HttpStatusCode.OK, """
            { "items": [ { "path": "src/use-case.ts", "html_url": "https://github.com/x", "text_matches": [ { "fragment": "class UseCase" } ] } ] }
            """);

        var matches = await Harness.Client.SearchCode(Context, Repository, "UseCase", TestContext.Current.CancellationToken);

        matches.Should().ContainSingle().Which.Fragments.Should().Equal("class UseCase");
        Harness.Http.Requests[^1].Accept.Should().Be("application/vnd.github.text-match+json");
    }

    [Fact]
    public async Task ListPullRequestFiles_ShouldMapEachChangedFile()
    {
        Harness.Http.On(HttpMethod.Get, "/repos/acme/netptune/pulls/16/files?per_page=100", HttpStatusCode.OK, """
            [ { "filename": "src/app.ts", "status": "modified", "additions": 3, "deletions": 1, "patch": "@@ -1 +1 @@" } ]
            """);

        var files = await Harness.Client.ListPullRequestFiles(Context, Repository, "16", TestContext.Current.CancellationToken);

        var file = files.Should().ContainSingle().Subject;

        file.Path.Should().Be("src/app.ts");
        file.Additions.Should().Be(3);
        file.Patch.Should().Be("@@ -1 +1 @@");
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

    private void StubFile(string path, string json)
    {
        Harness.Http.On(HttpMethod.Get, $"/repos/acme/netptune/contents/{path}?ref={BaseSha}", HttpStatusCode.OK, json);
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
