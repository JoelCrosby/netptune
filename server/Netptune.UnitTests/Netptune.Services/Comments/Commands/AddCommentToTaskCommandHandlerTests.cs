using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Services.Comments.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Comments.Commands;

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
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(10);
        UnitOfWork.Comments.GetCommentViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(2);
        UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns([]);

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var request = Fixture.Create<AddCommentRequest>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(2);

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Create<AddCommentRequest>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(10);
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
