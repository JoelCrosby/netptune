using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Services.Tags.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Commands;

public class AddTagToTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly AddTagToTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public AddTagToTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task AddToTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddTagToTaskRequest>().Create();
        var tag = AutoFixtures.Tag;
        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(viewModel);
        UnitOfWork.Tags.GetByValue(Arg.Any<string>(), Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).Returns(tag);
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.InvokeTransaction<ClientResponse<TagViewModel>>();

        var result = await Handler.Handle(new AddTagToTaskCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddToTask_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<AddTagToTaskRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.InvokeTransaction<ClientResponse<TagViewModel>>();

        var result = await Handler.Handle(new AddTagToTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddToTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var request = Fixture.Build<AddTagToTaskRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.InvokeTransaction<ClientResponse<TagViewModel>>();

        var result = await Handler.Handle(new AddTagToTaskCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
