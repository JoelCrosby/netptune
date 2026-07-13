namespace Netptune.Events;

public sealed record EventRetryPolicy
{
    public static readonly EventRetryPolicy Default = new();

    public TimeSpan AckWait { get; init; } = TimeSpan.FromSeconds(30);

    public int MaxDeliver { get; init; } = 5;

    // Multiples of AckWait, one per redelivery after the first. JetStream requires fewer entries than
    // MaxDeliver; the last one covers every delivery past the end of the schedule.
    public IReadOnlyList<int> RetrySteps { get; init; } = [4, 10, 30];

    // In JetStream, Backoff IS the ack wait for each delivery — not a nak delay. Entry zero is therefore the
    // ceiling on how long a handler may run: any shorter and the message is redelivered to a second replica
    // while the first is still handling it. Deriving the schedule from AckWait is what prevents that.
    public List<TimeSpan> Backoff => [AckWait, .. RetrySteps.Select(step => AckWait * step)];
}
