using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Comments;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class CommentsEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public CommentsEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var response = await Client.GetAsync("api/comments/task/neo-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<CommentViewModel>>();

        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.GetAsync("api/comments/task/neo-1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new AddCommentRequest
        {
            Comment = "New Comment",
            SystemId = "neo-1",
        };

        var response = await Client.PostAsJsonAsync("api/comments/task", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Body.Should().Be(request.Comment);
        result.Payload.IsEdited.Should().BeFalse();

        var eventRecord = await GetCommentEvent(EventKeys.CommentCreated, result.Payload.Id);

        eventRecord.Should().NotBeNull();
        eventRecord!.SubjectType.Should().Be(EventEntityTypes.From(EntityType.Task));

        (await HasOutboxRecord(eventRecord.Id)).Should().BeTrue();
        (await GetNotificationCount(eventRecord.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInputNotValid()
    {
        var request = new AddCommentRequest
        {
            SystemId = "neo-1",
        };

        var response = await Client.PostAsJsonAsync("api/comments/task", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnUpdatedComment_WhenCommentBelongsToCurrentUser()
    {
        var createResponse = await Client.PostAsJsonAsync("api/comments/task", new AddCommentRequest
        {
            Comment = "Original comment",
            SystemId = "neo-1",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>();
        var request = new UpdateCommentRequest { Comment = "Updated comment" };

        var response = await Client.PutAsJsonAsync($"api/comments/{created.Payload!.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Body.Should().Be(request.Comment);
        result.Payload.UpdatedAt.Should().NotBeNull();
        result.Payload.IsEdited.Should().BeTrue();

        var eventRecord = await GetCommentEvent(EventKeys.CommentUpdated, result.Payload.Id);

        eventRecord.Should().NotBeNull();

        (await HasOutboxRecord(eventRecord!.Id)).Should().BeTrue();
        (await GetNotificationCount(eventRecord!.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenCommentDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync(
            "api/comments/1000",
            new UpdateCommentRequest { Comment = "Updated comment" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnCorrectly_WhenInputValid()
    {
        var createResponse = await Client.PostAsJsonAsync("api/comments/task", new AddCommentRequest
        {
            Comment = "Comment to delete",
            SystemId = "neo-1",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>();

        var response = await Client.DeleteAsync($"api/comments/{created.Payload!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        var eventRecord = await GetCommentEvent(EventKeys.CommentDeleted, created.Payload.Id);

        eventRecord.Should().NotBeNull();

        (await HasOutboxRecord(eventRecord!.Id)).Should().BeTrue();
        (await GetNotificationCount(eventRecord!.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenInputDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/comments/1000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeFalse();
    }

    private async Task<Netptune.Core.Entities.EventRecord?> GetCommentEvent(string eventKey, int commentId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var commentIdValue = commentId.ToString();

        return await context.EventRecords
            .AsNoTracking()
            .Include(eventRecord => eventRecord.References)
            .SingleOrDefaultAsync(eventRecord =>
                eventRecord.EventKey == eventKey &&
                eventRecord.References.Any(reference =>
                    reference.EntityType == EventEntityTypes.From(EntityType.Comment) &&
                    reference.EntityId == commentIdValue));
    }

    private async Task<bool> HasOutboxRecord(long eventRecordId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.EventOutbox.AnyAsync(
            outbox => outbox.EventRecordId == eventRecordId,
            TestContext.Current.CancellationToken);
    }

    private async Task<int> GetNotificationCount(long eventRecordId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.Notifications.CountAsync(
            notification => notification.EventRecordId == eventRecordId,
            TestContext.Current.CancellationToken);
    }
}
