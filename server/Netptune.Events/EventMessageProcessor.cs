using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class UnknownMessageTypeException(string type)
    : Exception($"unknown message type {type}")
{
    public string Type { get; } = type;
}

// A message is never acked unless the handler handled it: a failure is left outstanding and redelivered on
// JetStream's own schedule, and a message that can never succeed is terminated. Neither is dropped.
public sealed class EventMessageProcessor
{
    private readonly EventRetryPolicy RetryPolicy;
    private readonly ILogger<EventMessageProcessor> Logger;

    public EventMessageProcessor(EventRetryPolicy retryPolicy, ILogger<EventMessageProcessor> logger)
    {
        RetryPolicy = retryPolicy;
        Logger = logger;
    }

    public async Task Process(
        INatsJSMsg<EventMessage> message,
        Func<EventMessage, CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        var payload = message.Data;

        var deliveryAttempt = (int) (message.Metadata?.NumDelivered ?? 1);

        if (payload is null)
        {
            await Terminate(message, "unreadable", deliveryAttempt, "the payload did not deserialize", cancellationToken);

            return;
        }

        try
        {
            await handler(payload, cancellationToken);

            await message.AckAsync(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnknownMessageTypeException exception)
        {
            await Terminate(message, payload.Type, deliveryAttempt, exception.Message, cancellationToken);
        }
        catch (Exception exception)
        {
            if (deliveryAttempt >= RetryPolicy.MaxDeliver)
            {
                await Terminate(
                    message,
                    payload.Type,
                    deliveryAttempt,
                    $"handler failed on delivery {deliveryAttempt} of {RetryPolicy.MaxDeliver}",
                    cancellationToken,
                    exception);

                return;
            }

            Logger.LogWarning(
                exception,
                "[Event] type {Type} failed on delivery {Attempt} of {MaxDeliver}, retrying",
                payload.Type,
                deliveryAttempt,
                RetryPolicy.MaxDeliver);

            // Deliberately neither acked nor naked. The consumer's Backoff schedule is the ack wait for each
            // delivery, so leaving the message outstanding is what makes JetStream redeliver it on that
            // schedule — an untimed NakAsync asks for it back at once and hot-loops through MaxDeliver.
        }
    }

    // Term, never Ack: it stops redelivery, leaves the message in the stream at its sequence, and emits the
    // CONSUMER.MSG_TERMINATED advisory that anything outside this process acts on. An ack would claim the
    // message was handled and destroy it silently.
    private async Task Terminate(
        INatsJSMsg<EventMessage> message,
        string type,
        int deliveryAttempt,
        string reason,
        CancellationToken cancellationToken,
        Exception? exception = null)
    {
        Logger.LogError(
            exception,
            "[Event] type {Type} from {Subject} terminated after {Deliveries} deliveries at stream sequence {Sequence}: {Reason}",
            type,
            message.Subject,
            deliveryAttempt,
            message.Metadata?.Sequence.Stream ?? 0,
            reason);

        await message.AckTerminateAsync(cancellationToken: cancellationToken);
    }
}
