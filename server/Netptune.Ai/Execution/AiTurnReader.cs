using System.Runtime.CompilerServices;

using Netptune.Ai.Providers;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution;

public static class AiTurnReader
{
    public static async IAsyncEnumerable<AiStreamEvent> Read(
        IAsyncEnumerable<AiStreamEvent> run,
        TimeSpan timeout,
        CancellationToken timeoutToken,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        string? timeoutMessage = null)
    {
        var turn = run.GetAsyncEnumerator(cancellationToken);

        bool hasTimedOut;
        bool wasStopped;
        string? failure;

        while (true)
        {
            var step = await ReadNext(turn, cancellationToken);

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
            yield return AiStreamEvent.Failed(timeoutMessage ?? DescribeTimeout(timeout));

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
        var isShort = timeout.TotalSeconds < 120;

        return isShort
            ? Localized.Format(AppText.AiErrorTimedOutSeconds, (int)timeout.TotalSeconds)
            : Localized.Format(AppText.AiErrorTimedOutMinutes, (int)timeout.TotalMinutes);
    }

    private sealed record TurnStep(AiStreamEvent? Event, string? Failure);

    private static async Task<TurnStep> ReadNext(
        IAsyncEnumerator<AiStreamEvent> turn,
        CancellationToken cancellationToken)
    {
        try
        {
            var moved = await turn.MoveNextAsync();

            return new TurnStep(moved ? turn.Current : null, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new TurnStep(null, null);
        }
        catch (OperationCanceledException)
        {
            // Nobody stopped the turn and it has not run out of time, so a client inside it gave up waiting.
            return new TurnStep(null, AppText.AiErrorProviderStoppedResponding);
        }
        catch (Exception exception)
        {
            var described = AiProviderErrors.Describe(exception);

            return new TurnStep(null, described ?? AppText.AiErrorProviderUnreachable);
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
