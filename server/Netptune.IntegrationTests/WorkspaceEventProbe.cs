using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Services.Realtime;

using StackExchange.Redis;

using Xunit;

namespace Netptune.IntegrationTests;

public static class WorkspaceEventProbe
{
    public const string RealtimeClientHeader = "X-Realtime-Client";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static async Task<bool> Published(
        IServiceProvider services,
        string sourceClientId,
        string scope,
        Func<Task> act)
    {
        var connection = services.GetRequiredService<IConnectionMultiplexer>();
        var channel = RedisChannel.Literal(IWorkspaceEventPublisher.ChannelName);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queue = await connection.GetSubscriber().SubscribeAsync(channel);

        queue.OnMessage(message =>
        {
            var published = JsonSerializer.Deserialize<JsonElement>(message.Message.ToString());
            var isFromSource = published.GetProperty("sourceClientId").GetString() == sourceClientId;
            var scopes = published.GetProperty("scopes").EnumerateArray().Select(item => item.GetString());
            var hasScope = scopes.Contains(scope);

            if (isFromSource && hasScope)
            {
                received.TrySetResult();
            }
        });

        try
        {
            await act();

            var delay = Task.Delay(Timeout, TestContext.Current.CancellationToken);
            var completed = await Task.WhenAny(received.Task, delay);

            return completed == received.Task;
        }
        finally
        {
            await connection.GetSubscriber().UnsubscribeAsync(channel);
        }
    }
}
