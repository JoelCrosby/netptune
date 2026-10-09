using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.Reporting.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class ReportingToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task FlowReport_ShouldPassTheRequestedWindowAndUnitThrough()
    {
        GetFlowReportQueryCaptured? captured = null;

        Mediator
            .Send(Arg.Any<GetFlowReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<GetFlowReportQuery>();

                captured = new GetFlowReportQueryCaptured(query.Filter);

                return ClientResponse<FlowReport>.Success(CreateFlowReport());
            });

        var tool = new GetReportTool(Mediator);
        var arguments = Arguments(
            """{"kind":"flow","projectId":3,"from":"2026-01-01","to":"2026-02-01","unit":"storyPoints","grouping":"week"}""");

        var result = await tool.Execute(arguments, TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured.Should().NotBeNull();
        captured!.Filter.ProjectId.Should().Be(3);
        captured.Filter.From.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        captured.Filter.To.Should().Be(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        captured.Filter.Unit.Should().Be(ReportingUnit.StoryPoints);
        captured.Filter.Grouping.Should().Be(ReportingGrouping.Week);
    }

    [Fact]
    public async Task FlowReport_ShouldReportTheHeadlineNumbers()
    {
        Mediator
            .Send(Arg.Any<GetFlowReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<FlowReport>.Success(CreateFlowReport()));

        var tool = new GetReportTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"flow"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("throughput").GetInt32().Should().Be(14);
        content.RootElement.GetProperty("currentOpenTaskCount").GetInt32().Should().Be(6);
        content.RootElement.GetProperty("completedPerBucket").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task FlowReport_ShouldFail_WhenTheReportCannotBeRead()
    {
        Mediator
            .Send(Arg.Any<GetFlowReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<FlowReport>.Failed("Bad range"));

        var tool = new GetReportTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"flow"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Bad range");
    }

    [Fact]
    public async Task Velocity_ShouldFail_WhenNoProjectIsGiven()
    {
        var tool = new GetReportTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"velocity"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("projectId");
    }

    [Fact]
    public async Task Burndown_ShouldFail_WhenNoSprintIsGiven()
    {
        var tool = new GetReportTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"burndown"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("sprintId");
    }

    [Fact]
    public async Task Report_ShouldFail_WhenTheReportIsUnknown()
    {
        var tool = new GetReportTool(Mediator);
        var result = await tool.Execute(Arguments("""{"kind":"forecast"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("flow, burndown, velocity, workload");
    }

    [Theory]
    [InlineData("flow", new[] { NetptunePermissions.Tasks.Read })]
    [InlineData("burndown", new[] { NetptunePermissions.Tasks.Read, NetptunePermissions.Sprints.Read })]
    [InlineData("velocity", new[] { NetptunePermissions.Tasks.Read, NetptunePermissions.Sprints.Read })]
    [InlineData("workload", new[] { NetptunePermissions.Tasks.Read, NetptunePermissions.Members.Read })]
    public void Report_ShouldRequireThePermissionsOfTheReportItReads(string report, string[] expected)
    {
        var tool = new GetReportTool(Mediator);
        var required = tool.GetRequiredPermissions(Arguments($$"""{"kind":"{{report}}"}"""));

        required.Should().BeEquivalentTo(expected);
    }

    private sealed record GetFlowReportQueryCaptured(ReportingFilter Filter);

    private static FlowReport CreateFlowReport()
    {
        return new FlowReport
        {
            Throughput = 14,
            MedianCycleTimeHours = 30.5m,
            P85CycleTimeHours = 96m,
            CycleTimeSampleSize = 12,
            CurrentOpenTaskCount = 6,
            Buckets = [new FlowBucket(new DateOnly(2026, 1, 5), 4)],
            CycleTimeBuckets = [],
            Coverage = new ReportingCoverage(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false),
        };
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
