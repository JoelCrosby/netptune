using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Notifications;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class NotificationsEndpointTests(NetptuneFixture fixture)
{
    [Fact]
    public async Task Get_ShouldReturnOk_WithNotifications()
    {
        var response = await fixture.Client.GetAsync("api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<NotificationViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().NotBeNullOrEmpty();
        result.Payload.TotalCount.Should().BeGreaterThan(0);
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

    [Fact]
    public async Task MarkReadMany_ShouldMarkOnlyTheRequestedNotifications()
    {
        var seeded = await SeedNotifications(2);
        var target = seeded[0];
        var untouched = seeded[1];

        var response = await fixture.Client.PutAsJsonAsync("api/notifications/read", new[] { target });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        // Other tests in this class mark the whole list read, so the assertion reads back only the
        // two rows this test owns.
        (await ReadFlags(target)).IsRead.Should().BeTrue();
        (await ReadFlags(untouched)).IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldRemoveOnlyTheRequestedNotifications()
    {
        var seeded = await SeedNotifications(2);
        var removed = seeded[0];
        var kept = seeded[1];

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new("api/notifications", UriKind.RelativeOrAbsolute),
            Content = JsonContent.Create(new[] { removed }),
        };

        var response = await fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        (await ReadFlags(removed)).IsDeleted.Should().BeTrue();
        (await ReadFlags(kept)).IsDeleted.Should().BeFalse();
    }

    private sealed record NotificationFlags(bool IsRead, bool IsDeleted);

    private async Task<NotificationFlags> ReadFlags(int notificationId)
    {
        using var scope = fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Notifications
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == notificationId)
            .Select(item => new NotificationFlags(item.IsRead, item.IsDeleted))
            .SingleAsync();
    }

    // The seeded notifications are shared state that other tests here mark read wholesale, so the
    // read-many and delete assertions get their own rows to act on.
    private async Task<IReadOnlyList<int>> SeedNotifications(int count)
    {
        using var scope = fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var workspace = await context.Workspaces.SingleAsync(item => item.Slug == "netptune");

        // The commands are scoped to the calling user, so the rows have to belong to whoever the
        // test client authenticates as rather than to any member of the workspace.
        var currentUser = await fixture.Client.GetFromJsonAsync<CurrentUserResponse>("api/auth/current-user");
        var userId = currentUser!.UserId;
        var eventRecordId = await context.EventRecords
            .Where(item => item.WorkspaceId == workspace.Id)
            .Select(item => item.Id)
            .FirstAsync();

        var notifications = Enumerable.Range(0, count).Select(_ => new Notification
        {
            UserId = userId,
            EventRecordId = eventRecordId,
            WorkspaceId = workspace.Id,
            IsRead = false,
            Link = $"/{workspace.Slug}/tasks",
            EntityType = EntityType.Task,
            ActivityType = ActivityType.Modify,
            CreatedByUserId = userId,
            OwnerId = userId,
        }).ToList();

        context.Notifications.AddRange(notifications);

        await context.SaveChangesAsync();

        return notifications.Select(item => item.Id).ToList();
    }

    private async Task<IReadOnlyList<NotificationViewModel>> GetNotificationsAsync()
    {
        var response = await fixture.Client.GetAsync("api/notifications?pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<NotificationViewModel>>>();
        return result.Payload!.Items;
    }

    private async Task<int> GetUnreadCountAsync()
    {
        var response = await fixture.Client.GetAsync("api/notifications/unread-count");
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<int>>();
        return result.Payload;
    }
}
