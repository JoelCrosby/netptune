using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Services.Comments.Commands;
using Netptune.Services.Comments.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class AddCommentToTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly AddCommentToTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public AddCommentToTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddCommentRequest>().With(x => x.SystemId, "key-2").Create();
        var viewModel = Fixture.Build<CommentViewModel>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).Returns(10);
        UnitOfWork.Comments.GetCommentViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(2);

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), CancellationToken.None);

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

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), CancellationToken.None);

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

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class GetCommentsForTaskQueryHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly GetCommentsForTaskQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetCommentsForTaskQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var viewModels = new List<CommentViewModel> { Fixture.Create<CommentViewModel>() };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).Returns(1);
        UnitOfWork.Comments.GetCommentViewModelsForTask(Arg.Any<int>()).Returns(viewModels);

        var result = await Handler.Handle(new GetCommentsForTaskQuery("task-id"), CancellationToken.None);

        result.Should().NotBeEmpty();
        result!.Count.Should().Be(1);
        result.Should().BeEquivalentTo(viewModels);
    }

    [Fact]
    public async Task GetCommentsForTask_ShouldReturnNull_WhenTaskNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).ReturnsNull();

        var result = await Handler.Handle(new GetCommentsForTaskQuery("task-id"), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class DeleteCommentCommandHandlerTests
{
    private readonly DeleteCommentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteCommentCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Comments.GetAsync(1).Returns(AutoFixtures.Comment);

        var result = await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Comments.GetAsync(1).Returns(AutoFixtures.Comment);

        await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Comments.GetAsync(1).ReturnsNull();

        var result = await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Comments.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
