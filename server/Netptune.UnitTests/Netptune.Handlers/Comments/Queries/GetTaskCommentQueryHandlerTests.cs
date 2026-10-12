using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Comments.Queries;

public class GetTaskCommentQueryHandlerTests
{
    private const int TaskId = 9;
    private const int CommentId = 40;

    private readonly GetTaskCommentQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTaskCommentQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
        Identity.GetWorkspaceKey().Returns("key");
    }

    [Fact]
    public async Task GetTaskComment_ShouldReadTheCommentOnTheWorkspacesTask()
    {
        var comment = new CommentViewModel { Id = CommentId, Body = "Older comment" };

        UnitOfWork.Tasks.GetTaskInternalId("NPT-1", "key", TestContext.Current.CancellationToken).Returns(TaskId);
        UnitOfWork.Comments.GetTaskCommentViewModel(TaskId, CommentId, TestContext.Current.CancellationToken).Returns(comment);

        var result = await Handler.Handle(new GetTaskCommentQuery("NPT-1", CommentId), TestContext.Current.CancellationToken);

        result.Should().Be(comment);
    }

    [Fact]
    public async Task GetTaskComment_ShouldReturnNull_WhenTheTaskIsNotInTheWorkspace()
    {
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetTaskCommentQuery("NPT-1", CommentId), TestContext.Current.CancellationToken);

        result.Should().BeNull();
        await UnitOfWork.Comments.DidNotReceiveWithAnyArgs().GetTaskCommentViewModel(default, default, TestContext.Current.CancellationToken);
    }
}
