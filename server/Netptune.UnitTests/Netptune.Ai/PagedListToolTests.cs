using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Automations;
using Netptune.Core.ViewModels.Files;
using Netptune.Handlers.Automations.Queries;
using Netptune.Handlers.Storage.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class PagedListToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task ListWorkspaceFiles_ShouldAskForThePage_AndSayWhetherThereAreMore()
    {
        var file = new WorkspaceFileViewModel { Id = 5, OriginalName = "spec.pdf", ContentUrl = "/files/5" };
        var page = new PagedResponse<WorkspaceFileViewModel>([file], 2, 10, 25);

        Mediator
            .Send(Arg.Any<GetWorkspaceFilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<WorkspaceFileViewModel>>.Success(page));

        var result = await Execute(new ListWorkspaceFilesTool(Mediator), """{"page":2,"pageSize":10}""");

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;

        root.GetProperty("page").GetInt32().Should().Be(2);
        root.GetProperty("totalCount").GetInt32().Should().Be(25);
        root.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        await Mediator.Received(1).Send(
            Arg.Is<GetWorkspaceFilesQuery>(query => query.Filter.Page == 2 && query.Filter.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ListWorkspaceFiles_ShouldRequireStorageRead_LikeTheStorageFilesRoute()
    {
        IAiTool tool = new ListWorkspaceFilesTool(Mediator);

        tool.RequiredPermissions.Should().BeEquivalentTo([NetptunePermissions.Storage.Read]);
        tool.IsAvailable(new HashSet<string> { NetptunePermissions.Files.Read }).Should().BeFalse();
    }

    [Fact]
    public async Task ListWorkspaceFiles_ShouldSayThereAreNoMore_OnTheLastPage()
    {
        var page = new PagedResponse<WorkspaceFileViewModel>([], 3, 10, 25);

        Mediator
            .Send(Arg.Any<GetWorkspaceFilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<WorkspaceFileViewModel>>.Success(page));

        var result = await Execute(new ListWorkspaceFilesTool(Mediator), """{"page":3,"pageSize":10}""");

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("hasMore").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ListAutomationRuns_ShouldAskForThePage_AndWrapTheRunsWithTheirCount()
    {
        var run = new AutomationRunViewModel { Id = 9, AutomationRuleId = 4, IdempotencyKey = "k" };
        var page = new PagedResponse<AutomationRunViewModel>([run], 1, 1, 3);

        Mediator
            .Send(Arg.Any<GetAutomationRunsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<AutomationRunViewModel>>.Success(page));

        var result = await Execute(new ListAutomationRunsTool(Mediator), """{"ruleId":4,"pageSize":1}""");

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;

        root.GetProperty("totalCount").GetInt32().Should().Be(3);
        root.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        root.GetProperty("runs").EnumerateArray().Single().GetProperty("id").GetInt32().Should().Be(9);
        await Mediator.Received(1).Send(
            Arg.Is<GetAutomationRunsQuery>(query => query.RuleId == 4 && query.Filter.Page == 1 && query.Filter.PageSize == 1),
            Arg.Any<CancellationToken>());
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string arguments)
    {
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }
}
