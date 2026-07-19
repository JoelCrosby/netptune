using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;

namespace Netptune.Events;

public sealed class EventStream
{
    private readonly INatsJSContext JetStream;
    private readonly SemaphoreSlim Gate = new(1, 1);

    private bool Created;
    private bool CanonicalCreated;

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
            if (Created)
            {
                return;
            }

            await EnsureLegacyCreated(cancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task EnsureCanonicalCreated(CancellationToken cancellationToken = default)
    {
        if (CanonicalCreated)
        {
            return;
        }

        await Gate.WaitAsync(cancellationToken);

        try
        {
            if (CanonicalCreated)
            {
                return;
            }

            // Existing installations used `netptune.>` for the legacy stream. Narrow that stream before
            // creating the canonical one, otherwise JetStream rejects `netptune.events.v1.>` as overlapping.
            await EnsureLegacyCreated(cancellationToken);

            await JetStream.CreateOrUpdateStreamAsync(new StreamConfig
            {
                Name = MessageKeys.CanonicalQueue,
                Subjects = [MessageKeys.Subjects.Canonical],
                Storage = StreamConfigStorage.File,
            }, cancellationToken);

            CanonicalCreated = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task EnsureLegacyCreated(CancellationToken cancellationToken)
    {
        if (Created)
        {
            return;
        }

        await JetStream.CreateOrUpdateStreamAsync(new StreamConfig
        {
            Name = MessageKeys.Queue,
            Subjects = [.. MessageKeys.Subjects.Legacy],
            Storage = StreamConfigStorage.File,
        }, cancellationToken);

        Created = true;
    }
}
