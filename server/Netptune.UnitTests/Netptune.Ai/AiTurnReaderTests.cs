using System.Runtime.CompilerServices;

using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiTurnReaderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task Read_ShouldPassEventsThrough_WhenTheTurnCompletes()
    {
        using var timeout = new CancellationTokenSource();
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        var events = await Drain(Completes(), timeout, turn);

        events.Select(item => item.Type).Should().Equal(
            AiStreamEventType.TextDelta,
            AiStreamEventType.TurnCompleted);
    }

    [Fact]
    public async Task Read_ShouldReportTheTimeout_WhenTheTurnRunsOutOfTime()
    {
        using var timeout = new CancellationTokenSource();
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        var events = await Drain(HangsAfterDelta(timeout, cancelledAs: null), timeout, turn);

        events.Select(item => item.Type).Should().Equal(AiStreamEventType.TextDelta, AiStreamEventType.Error);
        events[^1].Message.Should().Be(AiTurnReader.DescribeTimeout(Timeout));
    }

    [Fact]
    public async Task Read_ShouldReportTheTimeoutOnly_WhenTheProviderSurfacesItAsItsOwnFailure()
    {
        using var timeout = new CancellationTokenSource();
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        var run = HangsAfterDelta(timeout, cancelledAs: new HttpRequestException("connection closed"));
        var events = await Drain(run, timeout, turn);

        events.Where(item => item.Type == AiStreamEventType.Error)
            .Should().ContainSingle()
            .Which.Message.Should().Be(AiTurnReader.DescribeTimeout(Timeout));
    }

    [Fact]
    public async Task Read_ShouldReportStopped_WhenTheUserStopsTheTurn()
    {
        using var timeout = new CancellationTokenSource();
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        var events = await Drain(HangsAfterDelta(turn, cancelledAs: null), timeout, turn);

        events.Select(item => item.Type).Should().Equal(AiStreamEventType.TextDelta, AiStreamEventType.Stopped);
    }

    [Fact]
    public async Task Read_ShouldReportTheProviderFailure_WhenTheProviderThrows()
    {
        using var timeout = new CancellationTokenSource();
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        var events = await Drain(Throws(new InvalidOperationException("boom")), timeout, turn);

        events.Should().ContainSingle()
            .Which.Message.Should().Be("The assistant could not reach the provider.");
    }

    [Theory]
    [InlineData(90, "after 90 seconds")]
    [InlineData(900, "after 15 minutes")]
    public void DescribeTimeout_ShouldUseTheUnitThatReadsNaturally(int seconds, string expected)
    {
        var message = AiTurnReader.DescribeTimeout(TimeSpan.FromSeconds(seconds));

        message.Should().Contain(expected);
    }

    private static async Task<List<AiStreamEvent>> Drain(
        IAsyncEnumerable<AiStreamEvent> run,
        CancellationTokenSource timeout,
        CancellationTokenSource turn)
    {
        var events = new List<AiStreamEvent>();

        await foreach (var streamEvent in AiTurnReader.Read(run, Timeout, timeout.Token, turn.Token))
        {
            events.Add(streamEvent);
        }

        return events;
    }

    private static async IAsyncEnumerable<AiStreamEvent> Completes()
    {
        await Task.Yield();

        yield return AiStreamEvent.Delta("Hello");
        yield return new AiStreamEvent { Type = AiStreamEventType.TurnCompleted };
    }

    // Emits one delta, then cancels the given source and fails the way a provider stream does when
    // its request is cancelled: with OperationCanceledException, or with the provider's own exception.
    private static async IAsyncEnumerable<AiStreamEvent> HangsAfterDelta(
        CancellationTokenSource cancels,
        Exception? cancelledAs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return AiStreamEvent.Delta("Partial");

        await cancels.CancelAsync();

        if (cancelledAs is not null)
        {
            throw cancelledAs;
        }

        await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
    }

    private static async IAsyncEnumerable<AiStreamEvent> Throws(Exception exception)
    {
        await Task.Yield();

        throw exception;

#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
