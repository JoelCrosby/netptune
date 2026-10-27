using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Tools;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Reading;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class RepositoryToolsTests
{
    private const int WorkspaceId = 3;
    private const string Repository = "acme/api";

    private readonly ISourceControlReader Reader = Substitute.For<ISourceControlReader>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public RepositoryToolsTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
    }

    [Fact]
    public async Task ReadFile_ShouldNumberTheRequestedLines()
    {
        GivenFile("one\ntwo\nthree\nfour\n");

        var result = await Execute(
            new ReadRepositoryFileTool(Reader, Identity),
            """{"repository":"acme/api","path":"src/app.ts","startLine":2,"endLine":3}""");

        using var content = JsonDocument.Parse(result.Content);

        result.IsError.Should().BeFalse();
        content.RootElement.GetProperty("content").GetString().Should().Be("2: two\n3: three\n");
        content.RootElement.GetProperty("totalLines").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task ReadFile_ShouldShortenALineLongerThanTheBudget_RatherThanPointBackAtIt()
    {
        GivenFile(new string('x', 40_000) + "\nsecond line\n");

        var tool = new ReadRepositoryFileTool(Reader, Identity);
        var result = await Execute(tool, """{"repository":"acme/api","path":"bundle.min.js"}""");
        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;

        root.GetProperty("content").GetString().Should().Contain("line shortened");
        root.GetProperty("endLine").GetInt32().Should().Be(1);
        root.GetProperty("continueFromLine").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ReadFile_ShouldSayWhereToContinueWhenALongFileIsCutOff()
    {
        var longLine = new string('x', 1_000);
        GivenFile(string.Join('\n', Enumerable.Repeat(longLine, 100)));

        var result = await Execute(new ReadRepositoryFileTool(Reader, Identity), """{"repository":"acme/api","path":"big.txt"}""");

        using var content = JsonDocument.Parse(result.Content);
        var continueFrom = content.RootElement.GetProperty("continueFromLine").GetInt32();

        result.Truncated.Should().BeTrue();
        continueFrom.Should().BeGreaterThan(1).And.BeLessThan(100);
    }

    [Fact]
    public async Task ReadFile_ShouldSayHowLongTheFileIsWhenAskedForALineBeyondTheEnd()
    {
        GivenFile("one\ntwo\n");

        var result = await Execute(
            new ReadRepositoryFileTool(Reader, Identity),
            """{"repository":"acme/api","path":"src/app.ts","startLine":3}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("has 2 lines");
    }

    [Fact]
    public void Tools_ShouldBeUnavailableWhenNoProviderIsEnabled()
    {
        Reader.HasProviders.Returns(false);

        var tool = new ReadRepositoryFileTool(Reader, Identity);
        var available = tool.IsAvailable(new HashSet<string> { "source_control.read" });

        available.Should().BeFalse();
    }

    [Fact]
    public async Task ReadFile_ShouldRefuseABinaryFile()
    {
        GivenFile(text: null, isBinary: true);

        var result = await Execute(new ReadRepositoryFileTool(Reader, Identity), """{"repository":"acme/api","path":"logo.png"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("binary");
    }

    [Fact]
    public async Task ReadFile_ShouldRequireARepository()
    {
        var result = await Execute(new ReadRepositoryFileTool(Reader, Identity), """{"path":"src/app.ts"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("list_repositories");
    }

    [Fact]
    public async Task ListFiles_ShouldOnlyListFilesUnderThePath()
    {
        var tree = new ScmTree
        {
            CommitSha = "abc",
            Entries =
            [
                new ScmTreeEntry("src", true, null),
                new ScmTreeEntry("src/app.ts", false, 10),
                new ScmTreeEntry("srcs/other.ts", false, 10),
                new ScmTreeEntry("README.md", false, 10),
            ],
        };

        Reader
            .ListFiles(WorkspaceId, Repository, null, Arg.Any<CancellationToken>())
            .Returns(ClientResponse<ScmRepositoryFiles>.Success(new ScmRepositoryFiles
            {
                Repository = Repository,
                Reference = "main",
                Tree = tree,
            }));

        var result = await Execute(new ListRepositoryFilesTool(Reader, Identity), """{"repository":"acme/api","path":"src"}""");

        using var content = JsonDocument.Parse(result.Content);
        var paths = content.RootElement.GetProperty("files").EnumerateArray()
            .Select(file => file.GetProperty("path").GetString())
            .ToList();

        paths.Should().Equal("src/app.ts");
    }

    [Fact]
    public async Task GetPullRequest_ShouldShortenLongDiffsAndAcceptAHash()
    {
        var detail = new ScmPullRequestDetail
        {
            PullRequest = new ScmPullRequest
            {
                ExternalId = "1",
                Number = "16",
                Title = "Fix search",
                SourceBranch = "feature/x",
                TargetBranch = "main",
                Author = new ScmIdentity { Username = "octocat" },
                WebUrl = "https://github.com/acme/api/pull/16",
            },
            Files = [new ScmPullRequestFile { Path = "src/app.ts", Status = "modified", Patch = new string('+', 10_000) }],
        };

        Reader
            .GetPullRequest(WorkspaceId, Repository, "16", Arg.Any<CancellationToken>())
            .Returns(ClientResponse<ScmPullRequestDetail>.Success(detail));

        var result = await Execute(new GetPullRequestTool(Reader, Identity), """{"repository":"acme/api","number":"#16"}""");

        using var content = JsonDocument.Parse(result.Content);
        var patch = content.RootElement.GetProperty("files")[0].GetProperty("patch").GetString();

        patch!.Length.Should().BeLessThan(4_000);
        patch.Should().EndWith("(diff shortened)");
    }

    [Fact]
    public async Task SearchCode_ShouldPassOnTheReadersReason_WhenItFails()
    {
        Reader
            .SearchCode(WorkspaceId, Repository, "UseCase", Arg.Any<CancellationToken>())
            .Returns(ClientResponse<IReadOnlyList<ScmCodeMatch>>.Failed("No linked repository is named acme/api."));

        var result = await Execute(new SearchRepositoryCodeTool(Reader, Identity), """{"repository":"acme/api","query":"UseCase"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Be("No linked repository is named acme/api.");
    }

    private void GivenFile(string? text, bool isBinary = false)
    {
        var file = new ScmRepositoryFile
        {
            Repository = Repository,
            Reference = "main",
            CommitSha = "abc",
            Content = new ScmFileContent { Path = "src/app.ts", Text = text, IsBinary = isBinary, WebUrl = "https://x" },
        };

        Reader
            .ReadFile(WorkspaceId, Repository, Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<ScmRepositoryFile>.Success(file));
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string json)
    {
        using var arguments = JsonDocument.Parse(json);

        var result = await tool.Execute(arguments.RootElement, TestContext.Current.CancellationToken);

        return result;
    }
}
