using System.Net;

using FluentAssertions;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Registry;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Providers.AzureDevOps;

public sealed class AzureDevOpsSourceControlClientTests : IDisposable
{
    private const string Repositories = """
        {
          "count": 3,
          "value": [
            {
              "id": "5febef5a-833d-4e14-b9c0-14cb638f91e6",
              "name": "api",
              "defaultBranch": "refs/heads/main",
              "webUrl": "https://dev.azure.com/acme/Platform/_git/api",
              "isDisabled": false,
              "project": { "id": "p-1", "name": "Platform" }
            },
            {
              "id": "0d1a2b3c-0000-4000-8000-000000000002",
              "name": "legacy",
              "defaultBranch": "refs/heads/master",
              "webUrl": "https://dev.azure.com/acme/Platform/_git/legacy",
              "isDisabled": true,
              "project": { "id": "p-1", "name": "Platform" }
            },
            {
              "id": "0d1a2b3c-0000-4000-8000-000000000003",
              "name": "empty",
              "webUrl": "https://dev.azure.com/acme/Website/_git/empty",
              "project": { "id": "p-2", "name": "Website" }
            }
          ]
        }
        """;

    private readonly AzureDevOpsTestHarness Harness = new();
    private readonly ScmConnectionContext Context;

    public AzureDevOpsSourceControlClientTests()
    {
        var registry = new SourceControlProviderRegistry([Harness.Provider]);

        Context = registry.CreateContext(Harness.CreateConnection());
    }

    [Fact]
    public async Task ListRepositories_ShouldMapEachRepositoryAndStripTheBranchRef()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.RepositoriesPath, HttpStatusCode.OK, Repositories);

        var repositories = await ListRepositories();
        var api = repositories.First();

        api.Ref.Should().Be(new ScmRepositoryRef("5febef5a-833d-4e14-b9c0-14cb638f91e6", "Platform", "api"));
        api.FullName.Should().Be("Platform/api");
        api.DefaultBranch.Should().Be("main");
        api.WebUrl.Should().Be("https://dev.azure.com/acme/Platform/_git/api");
        api.IsPrivate.Should().BeTrue();
        api.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ListRepositories_ShouldSkipDisabledRepositories()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.RepositoriesPath, HttpStatusCode.OK, Repositories);

        var repositories = await ListRepositories();

        repositories.Select(repository => repository.FullName).Should().Equal("Platform/api", "Website/empty");
    }

    [Fact]
    public async Task ListRepositories_ShouldLeaveTheDefaultBranchEmptyForAnEmptyRepository()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.RepositoriesPath, HttpStatusCode.OK, Repositories);

        var repositories = await ListRepositories();

        repositories.Last().DefaultBranch.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRepositories_ShouldCallTheOrganisationWithTheStoredToken()
    {
        Harness.Http.On(HttpMethod.Get, AzureDevOpsTestHarness.RepositoriesPath, HttpStatusCode.OK, Repositories);

        await ListRepositories();

        var request = Harness.Http.Requests.Single();

        request.Uri.AbsoluteUri.Should().Be($"https://dev.azure.com{AzureDevOpsTestHarness.RepositoriesPath}");
        request.Authorization.Should().Be(AzureDevOpsTestHarness.BasicCredential());
    }

    [Fact]
    public async Task ListRepositories_ShouldThrowAnUnauthorisedProviderExceptionForTheSignInPage()
    {
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.RepositoriesPath,
            HttpStatusCode.NonAuthoritativeInformation,
            configure: AzureDevOpsTestHarness.RespondWithSignInPage);

        var list = async () => await ListRepositories();

        var thrown = await list.Should().ThrowAsync<ScmProviderException>();

        thrown.Which.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        thrown.Which.Message.Should().NotContain(AzureDevOpsTestHarness.Token);
    }

    [Fact]
    public async Task ListRepositories_ShouldReportRateLimitingFromARetryAfterHeader()
    {
        Harness.Http.On(
            HttpMethod.Get,
            AzureDevOpsTestHarness.RepositoriesPath,
            HttpStatusCode.OK,
            Repositories,
            response => response.Headers.Add("Retry-After", "10"));

        var list = async () => await ListRepositories();

        await list.Should().ThrowAsync<ScmRateLimitedException>();
    }

    [Fact]
    public async Task OtherReads_ShouldNotBeSupportedYet()
    {
        var repository = new ScmRepositoryRef("1", "Platform", "api");

        var listBranches = async () => await Harness.Client.ListBranches(
            Context,
            repository,
            TestContext.Current.CancellationToken);

        await listBranches.Should().ThrowAsync<NotSupportedException>();
    }

    public void Dispose()
    {
        Harness.Dispose();
    }

    private async Task<List<ScmRepository>> ListRepositories()
    {
        var repositories = new List<ScmRepository>();

        await foreach (var repository in Harness.Client.ListRepositories(Context, TestContext.Current.CancellationToken))
        {
            repositories.Add(repository);
        }

        return repositories;
    }
}
