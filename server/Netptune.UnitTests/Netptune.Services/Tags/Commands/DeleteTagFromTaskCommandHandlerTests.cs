using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tags.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Commands;

public class DeleteTagFromTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly DeleteTagFromTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTagFromTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task DeleteFromTask_ShouldReturnSuccess_WhenValidId()
    {
        var request = Fixture.Create<DeleteTagFromTaskRequest>() with
        {
            SystemId = "task-id",
            Tag = "tag",
        };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, "key", TestContext.Current.CancellationToken).Returns(1);

        var result = await Handler.Handle(new DeleteTagFromTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFromTask_ShouldCallCompleteAsync_WhenValidId()
    {
        var request = Fixture.Create<DeleteTagFromTaskRequest>() with
        {
            SystemId = "task-id",
            Tag = "tag",
        };

        var tag = AutoFixtures.Tag with { Id = 1, Name = request.Tag };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, "key", TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Tags.GetByValue(request.Tag, 1, cancellationToken: TestContext.Current.CancellationToken).Returns(tag);

        await Handler.Handle(new DeleteTagFromTaskCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Tags.Received(1).DeleteTagFromTask(1, 1, request.Tag, TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteFromTask_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Create<DeleteTagFromTaskRequest>() with
        {
            SystemId = "task-id",
            Tag = "tag",
        };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key", TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteTagFromTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
