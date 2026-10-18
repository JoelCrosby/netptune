using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Activity.Realtime;
using Netptune.Core.Services.Realtime;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

public sealed class WorkspaceRefreshQueueTests
{
    private readonly NetptuneFixture Fixture;

    public WorkspaceRefreshQueueTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Enqueue_ShouldPublishTheScopesToTheWorkspace()
    {
        var token = TestContext.Current.CancellationToken;

        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspace = await context.Workspaces.SingleAsync(item => item.Slug == "netptune", token);

        using var queue = new WorkspaceRefreshQueue(
            Fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            Fixture.Services.GetRequiredService<IWorkspaceEventPublisher>(),
            NullLogger<WorkspaceRefreshQueue>.Instance);

        await queue.StartAsync(token);

        var published = await WorkspaceEventProbe.Published(
            Fixture.Services,
            string.Empty,
            WorkspaceEventScopes.Activity,
            () =>
            {
                queue.Enqueue(workspace.Id, [WorkspaceEventScopes.Activity]);

                return Task.CompletedTask;
            });

        await queue.StopAsync(token);

        published.Should().BeTrue();
    }
}
