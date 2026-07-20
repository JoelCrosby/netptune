using Microsoft.AspNetCore.Http;

using Netptune.App.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Services;

public class BoardEventServiceExtensionsTests
{
    [Fact]
    public async Task BroadcastRequestAsync_BroadcastsWorkspaceWithRealtimeClientId()
    {
        var service = Substitute.For<IBoardEventService>();
        var context = new DefaultHttpContext();
        context.Request.Headers["workspace"] = "workspace-one";
        context.Request.Headers["X-Realtime-Client"] = "browser-one";

        await service.BroadcastRequestAsync(context);

        await service.Received(1).BroadcastAsync("workspace-one", "browser-one");
    }

    [Fact]
    public async Task BroadcastRequestAsync_UsesConnectionIdWhenRealtimeClientIdIsMissing()
    {
        var service = Substitute.For<IBoardEventService>();
        var context = new DefaultHttpContext();
        context.Connection.Id = "connection-one";
        context.Request.Headers["workspace"] = "workspace-one";

        await service.BroadcastRequestAsync(context);

        await service.Received(1).BroadcastAsync("workspace-one", "connection-one");
    }

    [Fact]
    public async Task BroadcastRequestAsync_DoesNotBroadcastWithoutWorkspace()
    {
        var service = Substitute.For<IBoardEventService>();
        var context = new DefaultHttpContext();

        await service.BroadcastRequestAsync(context);

        await service.DidNotReceiveWithAnyArgs().BroadcastAsync(default!, default!);
    }
}
