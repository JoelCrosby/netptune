using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Netptune.App.Services;

public class BoardEventService : IBoardEventService
{
    private readonly ConcurrentDictionary<string, List<Channel<bool>>> Groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock Lock = new();

    public async Task SubscribeAsync(string group, HttpResponse response, CancellationToken cancellationToken)
    {
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        await response.Body.FlushAsync(cancellationToken);

        var channel = Channel.CreateUnbounded<bool>();

        AddChannel(group, channel);

        try
        {
            await foreach (var _ in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await response.WriteAsync("data: update\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            RemoveChannel(group, channel);
        }
    }

    public async Task BroadcastAsync(string group)
    {
        if (!Groups.TryGetValue(group, out var channels)) return;

        List<Channel<bool>> snapshot;

        lock (Lock)
        {
            snapshot = [..channels];
        }

        foreach (var channel in snapshot)
        {
            await channel.Writer.WriteAsync(true);
        }
    }

    private void AddChannel(string group, Channel<bool> channel)
    {
        lock (Lock)
        {
            if (!Groups.TryGetValue(group, out var channels))
            {
                channels = [];
                Groups[group] = channels;
            }

            channels.Add(channel);
        }
    }

    private void RemoveChannel(string group, Channel<bool> channel)
    {
        lock (Lock)
        {
            if (!Groups.TryGetValue(group, out var channels)) return;

            channels.Remove(channel);

            if (channels.Count == 0)
            {
                Groups.TryRemove(group, out _);
            }
        }
    }
}
