using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class CommentServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly CommentService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public CommentServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Create<AddCommentRequest>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).Returns(10);
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(2);

        var result = await Service.AddCommentToTask(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var request = Fixture.Create<AddCommentRequest>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).ReturnsNull();
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(2);

        var result = await Service.AddCommentToTask(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Create<AddCommentRequest>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).Returns(10);
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();

        var result = await Service.AddCommentToTask(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var viewModels = new List<CommentViewModel>
        {
            Fixture.Create<CommentViewModel>(),
        };

        Identity.GetWorkspaceKey();
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).Returns(1);
        UnitOfWork.Comments.GetCommentViewModelsForTask(Arg.Any<int>()).Returns(viewModels);

        var result = await Service.GetCommentsForTask("task-id");

        result.Should().NotBeEmpty();
        result!.Count.Should().Be(1);
        result.Should().BeEquivalentTo(viewModels);
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnNull_WhenTaskNotFound()
    {
        var viewModels = new List<CommentViewModel>
        {
            Fixture.Create<CommentViewModel>(),
        };

        Identity.GetWorkspaceKey();
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).ReturnsNull();
        UnitOfWork.Comments.GetCommentViewModelsForTask(Arg.Any<int>()).Returns(viewModels);

        var result = await Service.GetCommentsForTask("task-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var comment = AutoFixtures.Comment;

        UnitOfWork.Comments.GetAsync(1).Returns(comment);

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var comment = AutoFixtures.Comment;

        UnitOfWork.Comments.GetAsync(1).Returns(comment);

        await Service.Delete(1);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Comments.GetAsync(1).ReturnsNull();

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Comments.GetAsync(1).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
