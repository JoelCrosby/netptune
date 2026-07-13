using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class EventStream
{
    private readonly INatsJSContext JetStream;
    private readonly SemaphoreSlim Gate = new(1, 1);

    private bool Created;

    public EventStream(INatsJSContext jetStream)
    {
        JetStream = jetStream;
    }

    public async Task EnsureCreated(CancellationToken cancellationToken = default)
    {
        if (Created) return;

        await Gate.WaitAsync(cancellationToken);

        try
        {
            if (Created) return;

            await JetStream.CreateOrUpdateStreamAsync(new StreamConfig
            {
                Name = MessageKeys.Queue,
                Subjects = [MessageKeys.Subjects.Typed],
                Storage = StreamConfigStorage.File,
            }, cancellationToken);

            Created = true;
        }
        finally
        {
            Gate.Release();
        }
    }
}
