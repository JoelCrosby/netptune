using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiSpendServiceTests
{
    private const int WorkspaceId = 7;
    private const string Model = "claude-sonnet-5";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IAiConversationRepository Conversations = Substitute.For<IAiConversationRepository>();

    public AiSpendServiceTests()
    {
        UnitOfWork.AiConversations.Returns(Conversations);
    }

    [Fact]
    public async Task GetStatus_ShouldNotBeOverCap_WhenTheWorkspaceHasNoCap()
    {
        var service = new AiSpendService(UnitOfWork);
        var status = await service.GetStatus(CreateWorkspace(null), TestContext.Current.CancellationToken);

        status.IsOverCap.Should().BeFalse();
        status.Cap.Should().BeNull();

        await Conversations
            .DidNotReceive()
            .GetModelTokens(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatus_ShouldBeOverCap_WhenSpendHasReachedTheCap()
    {
        GivenModelTokens(inputTokens: 1_000_000);

        var service = new AiSpendService(UnitOfWork);
        var status = await service.GetStatus(CreateWorkspace(3m), TestContext.Current.CancellationToken);

        status.MonthToDate.Should().Be(3m);
        status.IsOverCap.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatus_ShouldNotBeOverCap_WhenSpendIsBelowTheCap()
    {
        GivenModelTokens(inputTokens: 1_000_000);

        var service = new AiSpendService(UnitOfWork);
        var status = await service.GetStatus(CreateWorkspace(25m), TestContext.Current.CancellationToken);

        status.MonthToDate.Should().Be(3m);
        status.IsOverCap.Should().BeFalse();
    }

    [Fact]
    public async Task GetSummary_ShouldRankMembersBySpend()
    {
        var today = DateTime.UtcNow.Date;

        GivenSlices(
            CreateSlice("user-1", "Ana Ruiz", today, inputTokens: 1_000_000),
            CreateSlice("user-2", "Sam Okafor", today, inputTokens: 3_000_000));

        GivenConversationCounts(new AiConversationCount("user-1", 2), new AiConversationCount("user-2", 5));

        var service = new AiSpendService(UnitOfWork);
        var summary = await service.GetSummary(CreateWorkspace(25m), TestContext.Current.CancellationToken);

        summary.MonthToDate.Should().Be(12m);
        summary.Cap.Should().Be(25m);
        summary.Conversations.Should().Be(7);

        summary.Members.Select(member => member.UserDisplayName)
            .Should().ContainInOrder("Sam Okafor", "Ana Ruiz");

        summary.Members[0].Usage.Cost.Should().Be(9m);
        summary.Members[0].Conversations.Should().Be(5);
    }

    [Fact]
    public async Task GetSummary_ShouldFillEveryDayOfTheMonthSoFar()
    {
        var today = DateTime.UtcNow.Date;

        GivenSlices(CreateSlice("user-1", "Ana Ruiz", today, inputTokens: 1_000_000));
        GivenConversationCounts();

        var service = new AiSpendService(UnitOfWork);
        var summary = await service.GetSummary(CreateWorkspace(null), TestContext.Current.CancellationToken);

        summary.Daily.Should().HaveCount(today.Day);
        summary.Daily[0].Day.Should().Be(new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        summary.Daily[^1].Cost.Should().Be(3m);
        summary.Daily.Sum(day => day.Cost).Should().Be(3m);
    }

    [Fact]
    public async Task GetSummary_ShouldProjectTheMonthFromTheRateSoFar()
    {
        var today = DateTime.UtcNow.Date;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var expected = decimal.Round(3m / today.Day * daysInMonth, 2, MidpointRounding.AwayFromZero);

        GivenSlices(CreateSlice("user-1", "Ana Ruiz", today, inputTokens: 1_000_000));
        GivenConversationCounts();

        var service = new AiSpendService(UnitOfWork);
        var summary = await service.GetSummary(CreateWorkspace(null), TestContext.Current.CancellationToken);

        summary.Projected.Should().Be(expected);
    }

    private void GivenModelTokens(int inputTokens)
    {
        var tokens = new List<AiModelTokens>
        {
            new()
            {
                Model = Model,
                InputTokens = inputTokens,
            },
        };

        Conversations
            .GetModelTokens(WorkspaceId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tokens));
    }

    private void GivenSlices(params AiSpendSlice[] slices)
    {
        Conversations
            .GetSpendSlices(WorkspaceId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(slices.ToList()));
    }

    private void GivenConversationCounts(params AiConversationCount[] counts)
    {
        Conversations
            .GetConversationCounts(WorkspaceId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(counts.ToList()));
    }

    private static AiSpendSlice CreateSlice(string userId, string displayName, DateTime day, int inputTokens)
    {
        return new AiSpendSlice
        {
            UserId = userId,
            UserDisplayName = displayName,
            Model = Model,
            Day = day,
            InputTokens = inputTokens,
        };
    }

    private static Workspace CreateWorkspace(decimal? cap)
    {
        return new Workspace
        {
            Id = WorkspaceId,
            Name = "Netptune",
            Slug = "netptune",
            AssistantSpendCap = cap,
        };
    }
}
