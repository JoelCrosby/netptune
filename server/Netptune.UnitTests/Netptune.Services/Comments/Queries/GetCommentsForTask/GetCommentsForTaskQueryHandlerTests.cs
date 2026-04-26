using AutoFixture;

using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Services.Comments.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Comments.Queries;

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
