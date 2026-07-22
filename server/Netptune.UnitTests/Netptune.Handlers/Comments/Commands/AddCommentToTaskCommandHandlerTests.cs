using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Events;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Comments.Commands;

public class AddCommentToTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly AddCommentToTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();

    public AddCommentToTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, EventRecords);
        UnitOfWork.Transaction(Arg.Any<Func<Task>>(), Arg.Any<bool>())
            .Returns(call => call.Arg<Func<Task>>()());
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

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<CommentEventPayload>>(eventRequest =>
                eventRequest.EventKey == EventKeys.CommentCreated &&
                eventRequest.SubjectType == "task" &&
                eventRequest.SubjectId == "10"),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCommentToTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var request = Fixture.Create<AddCommentRequest>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(2);

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), TestContext.Current.CancellationToken);

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

        var result = await Handler.Handle(new AddCommentToTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
