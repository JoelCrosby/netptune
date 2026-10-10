using System.Runtime.CompilerServices;

using Netptune.Ai.Providers;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution;

public static class AiTurnReader
{
    public static async IAsyncEnumerable<AiStreamEvent> Read(
        IAsyncEnumerable<AiStreamEvent> run,
        TimeSpan timeout,
        CancellationToken timeoutToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var turn = run.GetAsyncEnumerator(cancellationToken);

        bool hasTimedOut;
        bool wasStopped;
        string? failure;

        while (true)
        {
            var step = await ReadNext(turn);

            if (step.Event is null)
            {
                hasTimedOut = timeoutToken.IsCancellationRequested;
                wasStopped = cancellationToken.IsCancellationRequested && !hasTimedOut;
                failure = step.Failure;

                break;
            }

            yield return step.Event;
        }

        await DisposeTurn(turn);

        if (hasTimedOut)
        {
            yield return AiStreamEvent.Failed(DescribeTimeout(timeout));

            yield break;
        }

        if (wasStopped)
        {
            yield return AiStreamEvent.Stopped();
        }

        if (failure is not null)
        {
            yield return AiStreamEvent.Failed(failure);
        }
    }

    public static string DescribeTimeout(TimeSpan timeout)
    {
        var limit = timeout.TotalSeconds < 120
            ? $"{(int)timeout.TotalSeconds} seconds"
            : $"{(int)timeout.TotalMinutes} minutes";

        return $"The assistant ran out of time after {limit}. Send another message to let it continue.";
    }

    private sealed record TurnStep(AiStreamEvent? Event, string? Failure);

    private static async Task<TurnStep> ReadNext(IAsyncEnumerator<AiStreamEvent> turn)
    {
        try
        {
            var moved = await turn.MoveNextAsync();

            return new TurnStep(moved ? turn.Current : null, null);
        }
        catch (OperationCanceledException)
        {
            return new TurnStep(null, null);
        }
        catch (Exception exception)
        {
            var described = AiProviderErrors.Describe(exception);

            return new TurnStep(null, described ?? "The assistant could not reach the provider.");
        }
    }

    private static async Task DisposeTurn(IAsyncEnumerator<AiStreamEvent> turn)
    {
        try
        {
            await turn.DisposeAsync();
        }
        catch (OperationCanceledException)
        {
            /* The turn was stopped, so its provider stream ends the same way. */
        }
    }
}
