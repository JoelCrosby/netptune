using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Notifications;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class NotificationsEndpointTests(NetptuneFixture fixture)
{
    [Fact]
    public async Task Get_ShouldReturnOk_WithNotifications()
    {
        var response = await fixture.Client.GetAsync("api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<NotificationViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnNotificationsWithExpectedFields()
    {
        var result = await GetNotifications();

        var notification = result.First();

        notification.Id.Should().BeGreaterThan(0);
        notification.Link.Should().NotBeNullOrEmpty();
        notification.ActorUserId.Should().NotBeNullOrEmpty();
        notification.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Get_ShouldReturnMixOfReadAndUnread()
    {
        var notifications = await GetNotifications();

        notifications.Should().Contain(n => n.IsRead);
        notifications.Should().Contain(n => !n.IsRead);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnOk_WithPositiveCount()
    {
        var response = await fixture.Client.GetAsync("api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<int>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MarkAsRead_ShouldDecreaseUnreadCount()
    {
        var countBefore = await GetUnreadCount();

        var notifications = await GetNotifications();
        var unread = notifications.First(n => !n.IsRead);

        await fixture.Client.PutAsync($"api/notifications/{unread.Id}/read", null);

        var countAfter = await GetUnreadCount();

        countAfter.Should().Be(countBefore - 1);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenCalledTwiceOnSameNotification()
    {
        var notifications = await GetNotifications();
        var unread = notifications.First(n => !n.IsRead);

        await fixture.Client.PutAsync($"api/notifications/{unread.Id}/read", null);
        var response = await fixture.Client.PutAsync($"api/notifications/{unread.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAll_ShouldReturnOk()
    {
        var response = await fixture.Client.PutAsync("api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAll_ShouldSetUnreadCountToZero()
    {
        await fixture.Client.PutAsync("api/notifications/read-all", null);

        var count = await GetUnreadCount();

        count.Should().Be(0);
    }

    [Fact]
    public async Task ReadAll_ShouldMarkAllNotificationsRead()
    {
        await fixture.Client.PutAsync("api/notifications/read-all", null);

        var notifications = await GetNotifications();

        notifications.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    [Fact]
    public async Task SingleRead_ShouldReturnOk_WhenNotificationExists()
    {
        var notifications = await GetNotifications();
        var notification = notifications.First();

        var response = await fixture.Client.PutAsync($"api/notifications/{notification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    private async Task<List<NotificationViewModel>> GetNotifications()
    {
        var response = await fixture.Client.GetAsync("api/notifications");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<NotificationViewModel>>>();
        return result.Payload!;
    }

    private async Task<int> GetUnreadCount()
    {
        var response = await fixture.Client.GetAsync("api/notifications/unread-count");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<int>>();
        return result.Payload;
    }
}
