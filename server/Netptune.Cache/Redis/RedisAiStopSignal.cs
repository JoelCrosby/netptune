using Netptune.Core.Services.Ai;

using StackExchange.Redis;

namespace Netptune.Cache.Redis;

public sealed class RedisAiStopSignal(IConnectionMultiplexer connection) : IAiStopSignal
{
    private static readonly RedisChannel Channel = RedisChannel.Literal("ai-stop");

    public Task Publish(Guid operationId)
    {
        return connection.GetSubscriber().PublishAsync(Channel, operationId.ToString());
    }

    public void Subscribe(Action<Guid> onStop)
    {
        connection.GetSubscriber().Subscribe(Channel, (_, value) =>
        {
            var isOperationId = Guid.TryParse(value.ToString(), out var operationId);

            if (isOperationId)
            {
                onStop(operationId);
            }
        });
    }
}
