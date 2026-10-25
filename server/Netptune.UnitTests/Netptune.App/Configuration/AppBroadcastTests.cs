using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Netptune.App.Configuration;
using Netptune.App.Services;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Configuration;

public sealed class AppBroadcastTests : IAsyncDisposable
{
    private readonly IBoardEventService BoardEvents = Substitute.For<IBoardEventService>();
    private readonly WebApplication App;

    public AppBroadcastTests()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton(BoardEvents);
        App = builder.Build();
    }

    [Fact]
    public async Task BroadcastsWhen_ShouldBroadcastWhenThePayloadChangedSomething()
    {
        App.MapPost("/", () => Results.Ok(ClientResponse<Change>.Success(new Change(true))))
            .BroadcastsWhen<Change>(change => change.Changed, WorkspaceEventScopes.SourceControl);

        await Invoke();

        await BoardEvents.Received(1).BroadcastAsync("workspace-one", Arg.Any<string>(), Arg.Any<string[]>());
    }

    [Fact]
    public async Task BroadcastsWhen_ShouldNotBroadcastWhenThePayloadChangedNothing()
    {
        App.MapPost("/", () => Results.Ok(ClientResponse<Change>.Success(new Change(false))))
            .BroadcastsWhen<Change>(change => change.Changed, WorkspaceEventScopes.SourceControl);

        await Invoke();

        await BoardEvents.DidNotReceiveWithAnyArgs().BroadcastAsync(default!, default!, default!);
    }

    [Fact]
    public async Task BroadcastsWhen_ShouldNotBroadcastAFailure()
    {
        App.MapPost("/", () => Results.BadRequest(ClientResponse<Change>.Failed("No.")))
            .BroadcastsWhen<Change>(_ => true, WorkspaceEventScopes.SourceControl);

        await Invoke();

        await BoardEvents.DidNotReceiveWithAnyArgs().BroadcastAsync(default!, default!, default!);
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
    }

    private async Task Invoke()
    {
        var endpoint = ((IEndpointRouteBuilder)App).DataSources
            .SelectMany(source => source.Endpoints)
            .Single();

        var context = new DefaultHttpContext
        {
            RequestServices = App.Services,
        };

        context.Request.Method = HttpMethods.Post;
        context.Request.Headers["workspace"] = "workspace-one";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);
    }

    public sealed record Change(bool Changed);
}
