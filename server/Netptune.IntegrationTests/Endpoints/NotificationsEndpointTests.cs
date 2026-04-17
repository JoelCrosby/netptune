using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.ViewModels.Notifications;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class NotificationsEndpointTests
{
    private readonly HttpClient Client;

    public NotificationsEndpointTests(NetptuneFixture fixture)
    {
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WithNotifications()
    {
        var response = await Client.GetAsync("api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<NotificationViewModel>>();

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnNotificationsWithExpectedFields()
    {
        var response = await Client.GetAsync("api/notifications");
        var result = await response.Content.ReadFromJsonAsync<List<NotificationViewModel>>();

        var notification = result!.First();

        notification.Id.Should().BeGreaterThan(0);
        notification.Link.Should().NotBeNullOrEmpty();
        notification.ActorUserId.Should().NotBeNullOrEmpty();
        notification.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Get_ShouldReturnMixOfReadAndUnread()
    {
        var response = await Client.GetAsync("api/notifications");
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationViewModel>>();

        notifications.Should().Contain(n => n.IsRead);
        notifications.Should().Contain(n => !n.IsRead);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnOk_WithPositiveCount()
    {
        var response = await Client.GetAsync("api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var count = await response.Content.ReadFromJsonAsync<int>();

        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenNotificationExists()
    {
        var notifications = await GetNotifications();
        var unread = notifications.First(n => !n.IsRead);

        var response = await Client.PutAsync($"api/notifications/{unread.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAsRead_ShouldDecreaseUnreadCount()
    {
        var countBefore = await GetUnreadCount();

        var notifications = await GetNotifications();
        var unread = notifications.First(n => !n.IsRead);

        await Client.PutAsync($"api/notifications/{unread.Id}/read", null);

        var countAfter = await GetUnreadCount();

        countAfter.Should().Be(countBefore - 1);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenCalledTwiceOnSameNotification()
    {
        var notifications = await GetNotifications();
        var unread = notifications.First(n => !n.IsRead);

        await Client.PutAsync($"api/notifications/{unread.Id}/read", null);
        var response = await Client.PutAsync($"api/notifications/{unread.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnOk()
    {
        var response = await Client.PutAsync("api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldSetUnreadCountToZero()
    {
        await Client.PutAsync("api/notifications/read-all", null);

        var count = await GetUnreadCount();

        count.Should().Be(0);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldMarkAllNotificationsRead()
    {
        await Client.PutAsync("api/notifications/read-all", null);

        var notifications = await GetNotifications();

        notifications.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    private async Task<List<NotificationViewModel>> GetNotifications()
    {
        var response = await Client.GetAsync("api/notifications");
        return (await response.Content.ReadFromJsonAsync<List<NotificationViewModel>>())!;
    }

    private async Task<int> GetUnreadCount()
    {
        var response = await Client.GetAsync("api/notifications/unread-count");
        return await response.Content.ReadFromJsonAsync<int>();
    }
}
