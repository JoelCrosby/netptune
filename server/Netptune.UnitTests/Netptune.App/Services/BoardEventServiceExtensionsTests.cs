using Microsoft.AspNetCore.Http;

using Netptune.App.Services;
using Netptune.Core.Services.Realtime;

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

        await service.BroadcastRequestAsync(context, WorkspaceEventScopes.Task);

        await service.Received(1).BroadcastAsync(
            "workspace-one",
            "browser-one",
            Arg.Is<string[]>(scopes => scopes.SequenceEqual(new[] { WorkspaceEventScopes.Task })));
    }

    [Fact]
    public async Task BroadcastRequestAsync_UsesConnectionIdWhenRealtimeClientIdIsMissing()
    {
        var service = Substitute.For<IBoardEventService>();
        var context = new DefaultHttpContext();
        context.Connection.Id = "connection-one";
        context.Request.Headers["workspace"] = "workspace-one";

        await service.BroadcastRequestAsync(context, WorkspaceEventScopes.Task);

        await service.Received(1).BroadcastAsync("workspace-one", "connection-one", Arg.Any<string[]>());
    }

    [Fact]
    public async Task BroadcastRequestAsync_BroadcastsWithoutScopes_WhenTheCallerNamesNone()
    {
        var service = Substitute.For<IBoardEventService>();
        var context = new DefaultHttpContext();
        context.Request.Headers["workspace"] = "workspace-one";

        await service.BroadcastRequestAsync(context);

        await service.Received(1).BroadcastAsync(
            "workspace-one",
            Arg.Any<string>(),
            Arg.Is<string[]>(scopes => scopes.Length == 0));
    }

    [Fact]
    public async Task BroadcastRequestAsync_DoesNotBroadcastWithoutWorkspace()
    {
        var service = Substitute.For<IBoardEventService>();
        var context = new DefaultHttpContext();

        await service.BroadcastRequestAsync(context, WorkspaceEventScopes.Task);

        await service.DidNotReceiveWithAnyArgs().BroadcastAsync(default!, default!, default!);
    }
}
