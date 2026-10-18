using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using NATS.Client.Core;

using Netptune.Core.Events;
using Netptune.Core.Events.SourceControl;

using Xunit;

namespace Netptune.IntegrationTests;

public static class AutomationTriggerProbe
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static async Task<bool> Published(
        IServiceProvider services,
        Func<SourceControlTaskEventMessage, bool> matches,
        Func<Task> act)
    {
        var connection = services.GetRequiredService<INatsConnection>();
        var token = TestContext.Current.CancellationToken;

        await using var subscription = await connection.SubscribeCoreAsync(
            MessageKeys.Subjects.Automation,
            serializer: NatsRawSerializer<byte[]>.Default,
            cancellationToken: token);

        await act();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);

        timeout.CancelAfter(Timeout);

        try
        {
            await foreach (var message in subscription.Msgs.ReadAllAsync(timeout.Token))
            {
                var trigger = ReadTrigger(message.Data ?? []);

                if (trigger is not null && matches(trigger))
                {
                    return true;
                }
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
        }

        return false;
    }

    private static SourceControlTaskEventMessage? ReadTrigger(byte[] data)
    {
        var envelope = JsonSerializer.Deserialize<EventMessage>(data);
        var isTrigger = envelope?.Type == typeof(SourceControlTaskEventMessage).FullName;

        return isTrigger ? JsonSerializer.Deserialize<SourceControlTaskEventMessage>(envelope!.Payload) : null;
    }
}
