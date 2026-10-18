using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Reading;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.IntegrationTests.TestServices;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class SourceControlReaderTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public SourceControlReaderTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task ListRepositories_ShouldOnlyIncludeLinkedRepositories()
    {
        var (workspaceId, linked, unlinked) = await ConnectWithOneLinkedRepo();

        using var scope = Fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISourceControlReader>();

        var repositories = await reader.ListRepositories(workspaceId, TestContext.Current.CancellationToken);
        var names = repositories.Select(repository => repository.FullName).ToList();

        names.Should().Contain(linked);
        names.Should().NotContain(unlinked);
    }

    [Fact]
    public async Task ReadFile_ShouldReadAFileOnTheDefaultBranch()
    {
        var (workspaceId, linked, _) = await ConnectWithOneLinkedRepo();

        using var scope = Fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISourceControlReader>();

        var result = await reader.ReadFile(workspaceId, linked, "README.md", null, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.CommitSha.Should().Be(TestSourceControlProvider.HeadSha);
        result.Payload.Content.Text.Should().Be(TestSourceControlProvider.ReadmeText);
    }

    [Fact]
    public async Task ReadFile_ShouldRefuseARepositoryThatIsNotLinked()
    {
        var (workspaceId, _, unlinked) = await ConnectWithOneLinkedRepo();

        using var scope = Fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISourceControlReader>();

        var result = await reader.ReadFile(workspaceId, unlinked, "README.md", null, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("No linked repository");
    }

    [Fact]
    public async Task ReadFile_ShouldReportABranchThatDoesNotExist()
    {
        var (workspaceId, linked, _) = await ConnectWithOneLinkedRepo();

        using var scope = Fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISourceControlReader>();

        var result = await reader.ReadFile(workspaceId, linked, "README.md", "nope", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Branch nope was not found");
    }

    [Fact]
    public async Task GetPullRequest_ShouldIncludeItsChangedFiles()
    {
        var (workspaceId, linked, _) = await ConnectWithOneLinkedRepo();

        using var scope = Fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISourceControlReader>();

        var result = await reader.GetPullRequest(workspaceId, linked, "7", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Payload!.Files.Should().ContainSingle().Which.Path.Should().Be("README.md");
    }

    private async Task<(int WorkspaceId, string Linked, string Unlinked)> ConnectWithOneLinkedRepo()
    {
        var account = $"read-{Guid.NewGuid():N}"[..20];
        var token = TestContext.Current.CancellationToken;

        var connect = await Client.PostAsJsonAsync(
            "api/source-control/connections/complete",
            new CompleteSourceControlConnectRequest
            {
                Provider = SourceControlProvider.GitLab,
                Parameters = new Dictionary<string, string> { ["account"] = account },
            },
            token);
        var connection = await connect.Content.ReadFromJsonAsync<ClientResponse<SourceControlConnectionViewModel>>(token);

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var repos = await context.SourceControlRepos
            .AsNoTracking()
            .Where(repo => repo.ConnectionId == connection.Payload!.Id)
            .OrderBy(repo => repo.FullName)
            .Select(repo => new { repo.Id, repo.FullName, repo.WorkspaceId })
            .ToListAsync(token);

        var linked = await Client.PutAsJsonAsync(
            $"api/source-control/repos/{repos[0].Id}/linked",
            new SetSourceControlRepoLinkedRequest { IsLinked = true },
            token);

        linked.StatusCode.Should().Be(HttpStatusCode.OK);

        return (repos[0].WorkspaceId, repos[0].FullName, repos[1].FullName);
    }
}
