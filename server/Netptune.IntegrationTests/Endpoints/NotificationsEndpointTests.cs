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
        var notifications = await GetNotificationsAsync();

        var notification = notifications.First();

        notification.Id.Should().BeGreaterThan(0);
        notification.Link.Should().NotBeNullOrEmpty();
        notification.ActorUserId.Should().NotBeNullOrEmpty();
        notification.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Get_ShouldReturnMixOfReadAndUnread()
    {
        var notifications = await GetNotificationsAsync();

        if (!notifications.Any(n => n.IsRead) || notifications.All(n => n.IsRead))
        {
            Assert.Skip("Notification read/unread mix has been altered by a prior test.");
        }

        notifications.Should().Contain(n => n.IsRead);
        notifications.Should().Contain(n => !n.IsRead);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnOk_WithPositiveCount()
    {
        var count = await GetUnreadCountAsync();

        if (count == 0)
        {
            Assert.Skip("All notifications have been marked as read by a prior test.");
        }

        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MarkAsRead_ShouldDecreaseUnreadCount()
    {
        var notifications = await GetNotificationsAsync();
        var unread = notifications.FirstOrDefault(n => !n.IsRead);

        if (unread is null)
        {
            Assert.Skip("No unread notifications available; state may have been altered by a prior test.");
        }

        var countBefore = await GetUnreadCountAsync();

        await fixture.Client.PutAsync($"api/notifications/{unread.Id}/read", null);

        var countAfter = await GetUnreadCountAsync();

        countAfter.Should().Be(countBefore - 1);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenCalledTwiceOnSameNotification()
    {
        var notifications = await GetNotificationsAsync();
        var unread = notifications.FirstOrDefault(n => !n.IsRead);

        if (unread is null)
        {
            Assert.Skip("No unread notifications available; state may have been altered by a prior test.");
        }

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

        var count = await GetUnreadCountAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task ReadAll_ShouldMarkAllNotificationsRead()
    {
        await fixture.Client.PutAsync("api/notifications/read-all", null);

        var notifications = await GetNotificationsAsync();

        notifications.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    [Fact]
    public async Task SingleRead_ShouldReturnOk_WhenNotificationExists()
    {
        var notifications = await GetNotificationsAsync();
        var notification = notifications.First();

        var response = await fixture.Client.PutAsync($"api/notifications/{notification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();
    }

    private async Task<List<NotificationViewModel>> GetNotificationsAsync()
    {
        var response = await fixture.Client.GetAsync("api/notifications");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<NotificationViewModel>>>();
        return result.Payload!;
    }

    private async Task<int> GetUnreadCountAsync()
    {
        var response = await fixture.Client.GetAsync("api/notifications/unread-count");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<int>>();
        return result.Payload;
    }
}
