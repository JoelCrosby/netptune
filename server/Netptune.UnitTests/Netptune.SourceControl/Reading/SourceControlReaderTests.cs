using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;

using Netptune.Core.Entities;
using Netptune.Core.Models.Repository;
using Netptune.Core.Repositories;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.UnitOfWork;
using Netptune.SourceControl.Reading;
using Netptune.SourceControl.Registry;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Reading;

public sealed class SourceControlReaderTests : IDisposable
{
    private const int WorkspaceId = 3;
    private const string HeadSha = "2222222222222222222222222222222222222222";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ISourceControlRepoRepository Repos = Substitute.For<ISourceControlRepoRepository>();
    private readonly ISourceControlClient Client = Substitute.For<ISourceControlClient>();
    private readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private readonly SourceControlReader Reader;

    public SourceControlReaderTests()
    {
        var provider = Substitute.For<ISourceControlProvider>();

        provider.Provider.Returns(SourceControlProvider.GitLab);
        provider.Capabilities.Returns((SourceControlCapabilities)~0);
        provider.Client.Returns(Client);
        provider.Connector.Returns(Substitute.For<ISourceControlConnector>());

        UnitOfWork.SourceControlRepos.Returns(Repos);
        Repos.GetReadableForWorkspace(WorkspaceId, Arg.Any<CancellationToken>()).Returns([LinkedRepo()]);

        Client
            .GetBranch(Arg.Any<ScmConnectionContext>(), Arg.Any<ScmRepositoryRef>(), "main", Arg.Any<CancellationToken>())
            .Returns(new ScmBranch("main", HeadSha, "https://scm.test/tree/main"));
        Client
            .GetFile(Arg.Any<ScmConnectionContext>(), Arg.Any<ScmRepositoryRef>(), Arg.Any<string>(), HeadSha, Arg.Any<CancellationToken>())
            .Returns(new ScmFileContent { Path = "README.md", Text = "hi", WebUrl = "https://scm.test/README.md" });

        Reader = new SourceControlReader(UnitOfWork, new SourceControlProviderRegistry([provider]), Cache);
    }

    [Fact]
    public async Task ReadFile_ShouldResolveTheBranchAndReadTheFileOnlyOnceWhenReadTwice()
    {
        var token = TestContext.Current.CancellationToken;

        await Reader.ReadFile(WorkspaceId, "acme/api", "README.md", null, token);
        var second = await Reader.ReadFile(WorkspaceId, "ACME/api", "/README.md", "main", token);

        second.Payload!.CommitSha.Should().Be(HeadSha);
        await Client.Received(1).GetBranch(
            Arg.Any<ScmConnectionContext>(),
            Arg.Any<ScmRepositoryRef>(),
            "main",
            Arg.Any<CancellationToken>());
        await Client.Received(1).GetFile(
            Arg.Any<ScmConnectionContext>(),
            Arg.Any<ScmRepositoryRef>(),
            "README.md",
            HeadSha,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadFile_ShouldUseACommitShaWithoutLookingUpABranch()
    {
        await Reader.ReadFile(WorkspaceId, "acme/api", "README.md", HeadSha, TestContext.Current.CancellationToken);

        await Client.DidNotReceiveWithAnyArgs().GetBranch(
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReadFile_ShouldExplainARateLimitInsteadOfThrowing()
    {
        Client
            .GetFile(Arg.Any<ScmConnectionContext>(), Arg.Any<ScmRepositoryRef>(), "big.json", HeadSha, Arg.Any<CancellationToken>())
            .Returns<ValueTask<ScmFileContent?>>(_ => throw new ScmRateLimitedException(SourceControlProvider.GitLab, DateTimeOffset.UtcNow));

        var result = await Reader.ReadFile(WorkspaceId, "acme/api", "big.json", null, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("rate limit");
    }

    [Theory]
    [InlineData("../../other-repo/contents/secrets.yml")]
    [InlineData("src/../../other-repo/README.md")]
    [InlineData("src/./app.ts")]
    [InlineData("src\\app.ts")]
    public async Task ReadFile_ShouldRefuseAPathThatCouldLeaveTheRepository(string path)
    {
        var result = await Reader.ReadFile(WorkspaceId, "acme/api", path, null, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await Client.DidNotReceiveWithAnyArgs().GetFile(
            default!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReadFile_ShouldRefuseABranchThatCouldLeaveTheRepository()
    {
        var result = await Reader.ReadFile(
            WorkspaceId,
            "acme/api",
            "README.md",
            "../../other-repo/branches/main",
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await Client.DidNotReceiveWithAnyArgs().GetBranch(
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("password repo:acme/other-repo")]
    [InlineData("password org:acme")]
    [InlineData("(token OR USER:someone)")]
    public async Task SearchCode_ShouldRefuseAQueryThatNamesAnotherScope(string query)
    {
        var result = await Reader.SearchCode(WorkspaceId, "acme/api", query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await Client.DidNotReceiveWithAnyArgs().SearchCode(
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SearchCode_ShouldAllowNarrowingQualifiers()
    {
        Client
            .SearchCode(Arg.Any<ScmConnectionContext>(), Arg.Any<ScmRepositoryRef>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await Reader.SearchCode(
            WorkspaceId,
            "acme/api",
            "UseCase path:src language:typescript",
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ListRepositories_ShouldMatchAProjectWhoseUrlUsesTheSshForm()
    {
        var projects = Substitute.For<IProjectRepository>();

        UnitOfWork.Projects.Returns(projects);
        projects
            .GetRepositoryUrls(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns([new ProjectRepositoryUrl("API", "git@scm.test:acme/api.git")]);

        var repositories = await Reader.ListRepositories(WorkspaceId, TestContext.Current.CancellationToken);

        repositories.Single().ProjectKeys.Should().Equal("API");
    }

    public void Dispose()
    {
        Cache.Dispose();
    }

    private static SourceControlRepo LinkedRepo()
    {
        return new SourceControlRepo
        {
            Id = 11,
            WorkspaceId = WorkspaceId,
            ConnectionId = 7,
            ExternalId = "42",
            Owner = "acme",
            Name = "api",
            FullName = "acme/api",
            DefaultBranch = "main",
            WebUrl = "https://scm.test/acme/api",
            IsLinked = true,
            IsAccessible = true,
            Connection = new SourceControlConnection
            {
                Id = 7,
                WorkspaceId = WorkspaceId,
                Provider = SourceControlProvider.GitLab,
                ExternalAccountId = "acme",
                AccountLogin = "acme",
                Status = ScmConnectionStatus.Active,
                GrantedCapabilities = (SourceControlCapabilities)~0,
            },
        };
    }
}
