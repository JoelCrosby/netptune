using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;
using Netptune.Handlers.NotificationSubscriptions.Commands;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class NotificationSubscriptionsEndpointTests
{
    private const string BoardIdentifier = "neovim";

    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public NotificationSubscriptionsEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Upsert_ShouldReviveTheSameRow_WhenSubscribingTwice()
    {
        var boardId = await GetBoardId();
        var first = await Subscribe(NotificationScope.Board, boardId, NotificationSubscriptionEvents.TaskAdded);
        var second = await Subscribe(NotificationScope.Board, boardId, NotificationSubscriptionEvents.All);

        second.Id.Should().Be(first.Id, "subscribing again edits the subscription rather than adding one");
        second.Events.Should().Be(NotificationSubscriptionEvents.All);

        var deleteResponse = await Client.DeleteAsync($"api/notification-subscriptions/{first.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var revived = await Subscribe(NotificationScope.Board, boardId, NotificationSubscriptionEvents.TaskUpdated);

        revived.Id.Should().Be(first.Id, "an unsubscribed row is revived rather than duplicated");

        var rows = await CountSubscriptions(NotificationScope.Board, boardId);

        rows.Should().Be(1);

        await Client.DeleteAsync($"api/notification-subscriptions/{first.Id}");
    }

    [Fact]
    public async Task GetAll_ShouldCarryTheScopeNameAndLink()
    {
        var boardId = await GetBoardId();
        var subscription = await Subscribe(NotificationScope.Board, boardId, NotificationSubscriptionEvents.TaskAdded);

        var subscriptions = await GetAll();
        var listed = subscriptions.Should().ContainSingle(item => item.Id == subscription.Id).Subject;

        listed.Name.Should().NotBeNullOrWhiteSpace();
        listed.Context.Should().NotBeNullOrWhiteSpace("a board carries the project it belongs to");
        listed.Link.Should().Be($"/netptune/boards/{BoardIdentifier}");

        await Client.DeleteAsync($"api/notification-subscriptions/{subscription.Id}");
    }

    [Fact]
    public async Task GetAll_ShouldDropTheSubscription_AfterUnsubscribing()
    {
        var boardId = await GetBoardId();
        var subscription = await Subscribe(NotificationScope.Board, boardId, NotificationSubscriptionEvents.All);

        var deleteResponse = await Client.DeleteAsync($"api/notification-subscriptions/{subscription.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var subscriptions = await GetAll();

        subscriptions.Should().NotContain(item => item.Id == subscription.Id);
    }

    [Fact]
    public async Task Upsert_ShouldFail_WhenNoEventIsChosen()
    {
        var boardId = await GetBoardId();
        var response = await Post(NotificationScope.Board, boardId, NotificationSubscriptionEvents.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upsert_ShouldNotFound_WhenTheScopeIsNotInTheWorkspace()
    {
        var response = await Post(NotificationScope.Board, 100000, NotificationSubscriptionEvents.TaskAdded);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldNotFound_WhenTheSubscriptionDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/notification-subscriptions/100000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<NotificationSubscriptionViewModel> Subscribe(
        NotificationScope scope,
        int scopeEntityId,
        NotificationSubscriptionEvents events)
    {
        var response = await Post(scope, scopeEntityId, events);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<NotificationSubscriptionViewModel>>();

        return result.Payload!;
    }

    private Task<HttpResponseMessage> Post(
        NotificationScope scope,
        int scopeEntityId,
        NotificationSubscriptionEvents events)
    {
        var request = new UpsertNotificationSubscriptionRequest
        {
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            Events = events,
        };

        return Client.PutAsJsonAsync("api/notification-subscriptions", request);
    }

    private async Task<List<NotificationSubscriptionViewModel>> GetAll()
    {
        var response = await Client.GetAsync("api/notification-subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<NotificationSubscriptionViewModel>>();

        return result!;
    }

    private async Task<int> CountSubscriptions(NotificationScope scope, int scopeEntityId)
    {
        using var serviceScope = Fixture.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.NotificationSubscriptions
            .CountAsync(subscription => subscription.Scope == scope && subscription.ScopeEntityId == scopeEntityId);
    }

    private async Task<int> GetBoardId()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var board = await context.Boards
            .Include(item => item.Workspace)
            .FirstAsync(item => item.Workspace!.Slug == "netptune" && item.Identifier == BoardIdentifier);

        return board.Id;
    }
}
