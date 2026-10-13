using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class TaskSweepToolTests
{
    private const string WrappedDescription = "Steps to reproduce were wrapped\nby hand across\nthree lines.";

    private static readonly TaskTextViewModel KeyTask = new()
    {
        Id = 1,
        SystemId = "NPT-1",
        Name = "Rotate the API key",
        ProjectName = "Core",
        Description = "Short.",
    };

    private static readonly TaskTextViewModel WrappedTask = new()
    {
        Id = 2,
        SystemId = "NPT-2",
        Name = "Fix login",
        ProjectName = "Core",
        Description = WrappedDescription,
    };

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task FindInTasks_ShouldScanNamesAndDescriptions_ByDefault()
    {
        GivenFoundTasks(total: 1, KeyTask);

        var result = await Execute(new FindInTasksTool(Mediator), """{"pattern":"api key","projectId":3}""");

        using var content = JsonDocument.Parse(result.Content);
        var task = content.RootElement.GetProperty("tasks").EnumerateArray().Single();
        var match = task.GetProperty("matches").EnumerateArray().Single();

        task.GetProperty("systemId").GetString().Should().Be("NPT-1");
        match.GetProperty("in").GetString().Should().Be("name");
        await Mediator.Received(1).Send(
            Arg.Is<FindTasksByTextQuery>(query =>
                query.Search.Pattern == new TextPattern("api key", false, false)
                && query.Search.ProjectId == 3
                && query.Search.InName
                && query.Search.InDescription
                && !query.Search.InComments),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<FindTaskCommentsByTextQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindInTasks_ShouldReadMatchingComments_OnlyForTheTasksOnThePage()
    {
        var docs = new TaskTextViewModel { Id = 3, SystemId = "NPT-3", Name = "Docs" };
        var comments = new TaskCommentMatches([new TaskCommentTextViewModel(70, 3, "Remember the api key.")], new() { [3] = 4 });

        GivenFoundTasks(total: 1, docs);
        Mediator
            .Send(Arg.Any<FindTaskCommentsByTextQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskCommentMatches>.Success(comments));

        var result = await Execute(new FindInTasksTool(Mediator), """{"pattern":"api key","in":["comments"],"pageSize":2}""");

        using var content = JsonDocument.Parse(result.Content);
        var task = content.RootElement.GetProperty("tasks").EnumerateArray().Single();
        var match = task.GetProperty("matches").EnumerateArray().Single();

        task.GetProperty("matchingComments").GetInt32().Should().Be(4);
        match.GetProperty("in").GetString().Should().Be("comment");
        match.GetProperty("commentId").GetInt32().Should().Be(70);
        await Mediator.Received(1).Send(
            Arg.Is<FindTaskCommentsByTextQuery>(query => query.TaskIds.SequenceEqual(new[] { 3 }) && query.Take == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindInTasks_ShouldReportTheLineOfAMatchDeepInTheDescription()
    {
        GivenFoundTasks(total: 1, WrappedTask);

        var result = await Execute(new FindInTasksTool(Mediator), """{"pattern":"three lines"}""");

        using var content = JsonDocument.Parse(result.Content);
        var match = content.RootElement.GetProperty("tasks").EnumerateArray().Single().GetProperty("matches").EnumerateArray().Single();

        match.GetProperty("in").GetString().Should().Be("description");
        match.GetProperty("line").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task FindInTasks_ShouldReportEveryMatchingTask_AndWhetherThereAreMore()
    {
        GivenFoundTasks(total: 30, KeyTask);

        var result = await Execute(new FindInTasksTool(Mediator), """{"pattern":"key","pageSize":1}""");

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("tasksMatched").GetInt32().Should().Be(30);
        content.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void FindInTasks_ShouldAskForCommentsRead_OnlyWhenScanningComments()
    {
        var tool = new FindInTasksTool(Mediator);
        var withComments = JsonDocument.Parse("""{"pattern":"x","in":["comments"]}""").RootElement;
        var withoutComments = JsonDocument.Parse("""{"pattern":"x"}""").RootElement;

        tool.GetRequiredPermissions(withComments).Should().Contain(NetptunePermissions.Comments.Read);
        tool.GetRequiredPermissions(withoutComments).Should().NotContain(NetptunePermissions.Comments.Read);
    }

    [Theory]
    [InlineData("""{"pattern":"x","in":["files"]}""", "cannot be scanned")]
    [InlineData("""{"pattern":"(","regex":true}""", "not a valid regular expression")]
    public async Task FindInTasks_ShouldRefuse_WhatItCannotScan(string arguments, string message)
    {
        var result = await Execute(new FindInTasksTool(Mediator), arguments);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain(message);
        await Mediator.DidNotReceive().Send(Arg.Any<FindTasksByTextQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTaskDescriptions_ShouldAskForOnePage_AndDescribeEachTask()
    {
        GivenTaskTexts(total: 3, KeyTask, WrappedTask);

        var result = await Execute(new ListTaskDescriptionsTool(Mediator), """{"page":1,"pageSize":2,"projectId":3}""");

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;
        var tasks = root.GetProperty("tasks").EnumerateArray().ToList();

        tasks[1].GetProperty("systemId").GetString().Should().Be("NPT-2");
        tasks[1].GetProperty("format").GetProperty("wrappedLines").GetInt32().Should().Be(2);
        tasks[1].GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("totalCount").GetInt32().Should().Be(3);
        root.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        await Mediator.Received(1).Send(
            Arg.Is<GetTaskTextsQuery>(query => query.ProjectId == 3 && query.Page.Page == 1 && query.Page.PageSize == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTaskDescriptions_ShouldIncludeFullDescriptions_WhenAskedTo()
    {
        GivenTaskTexts(total: 2, KeyTask, WrappedTask);

        var result = await Execute(new ListTaskDescriptionsTool(Mediator), """{"includeDescriptions":true}""");

        using var content = JsonDocument.Parse(result.Content);
        var descriptions = content.RootElement.GetProperty("tasks").EnumerateArray().Select(task => task.GetProperty("description").GetString());

        descriptions.Should().Equal("Short.", WrappedDescription);
    }

    [Fact]
    public async Task ListTaskDescriptions_ShouldLeaveOutRowsThatDoNotFit_AndSaySo()
    {
        var tasks = Enumerable.Range(1, 5)
            .Select(id => new TaskTextViewModel
            {
                Id = id,
                SystemId = $"NPT-{id}",
                Name = $"Task {id}",
                Description = new string('a', 10_000),
            })
            .ToArray();

        GivenTaskTexts(total: 5, tasks);

        var result = await Execute(new ListTaskDescriptionsTool(Mediator), """{"includeDescriptions":true}""");

        using var content = JsonDocument.Parse(result.Content);

        result.Content.Length.Should().BeLessThan(32_000);
        content.RootElement.GetProperty("tasks").GetArrayLength().Should().Be(2);
        content.RootElement.GetProperty("omittedForSize").GetInt32().Should().Be(3);
    }

    private void GivenFoundTasks(int total, params TaskTextViewModel[] tasks)
    {
        var found = new PagedResponse<TaskTextViewModel>(tasks, 1, 25, total);

        Mediator
            .Send(Arg.Any<FindTasksByTextQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<TaskTextViewModel>>.Success(found));
    }

    private void GivenTaskTexts(int total, params TaskTextViewModel[] tasks)
    {
        var texts = new PagedResponse<TaskTextViewModel>(tasks, 1, 50, total);

        Mediator
            .Send(Arg.Any<GetTaskTextsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<PagedResponse<TaskTextViewModel>>.Success(texts));
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string arguments)
    {
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }
}
