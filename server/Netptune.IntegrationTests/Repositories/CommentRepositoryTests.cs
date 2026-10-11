using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

using Xunit;

namespace Netptune.IntegrationTests.Repositories;

public sealed class CommentRepositoryTests
{
    private const string WorkspaceKey = "netptune";
    private const string CommentedTask = "neo-1";
    private const string OtherTask = "neo-2";

    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public CommentRepositoryTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task GetTaskCommentViewModel_ShouldReadTheComment_WhenItIsOnTheTask()
    {
        var commentId = await CreateComment();
        var taskId = await GetTaskId(CommentedTask);

        var comment = await ReadComment(taskId, commentId);

        comment.Should().NotBeNull();
        comment!.Body.Should().Be("Repository comment");
    }

    [Fact]
    public async Task GetTaskCommentViewModel_ShouldReturnNull_WhenTheCommentIsOnAnotherTask()
    {
        var commentId = await CreateComment();
        var otherTaskId = await GetTaskId(OtherTask);

        var comment = await ReadComment(otherTaskId, commentId);

        comment.Should().BeNull();
    }

    [Fact]
    public async Task GetTaskCommentViewModel_ShouldReturnNull_WhenTheCommentIsDeleted()
    {
        var commentId = await CreateComment();
        var taskId = await GetTaskId(CommentedTask);
        var deleted = await Client.DeleteAsync($"api/comments/{commentId}", TestContext.Current.CancellationToken);

        deleted.EnsureSuccessStatusCode();

        var comment = await ReadComment(taskId, commentId);

        comment.Should().BeNull();
    }

    private async Task<int> CreateComment()
    {
        var response = await Client.PostAsJsonAsync("api/comments/task", new AddCommentRequest
        {
            Comment = "Repository comment",
            SystemId = CommentedTask,
        }, TestContext.Current.CancellationToken);

        var created = await response.Content.ReadFromJsonAsync<ClientResponse<CommentViewModel>>(TestContext.Current.CancellationToken);

        return created.Payload!.Id;
    }

    private async Task<int> GetTaskId(string systemId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var taskId = await unitOfWork.Tasks.GetTaskInternalId(systemId, WorkspaceKey, TestContext.Current.CancellationToken);

        return taskId!.Value;
    }

    private async Task<CommentViewModel?> ReadComment(int taskId, int commentId)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var comment = await unitOfWork.Comments.GetTaskCommentViewModel(taskId, commentId, TestContext.Current.CancellationToken);

        return comment;
    }
}
