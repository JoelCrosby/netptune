using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Projects.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Projects.Commands;

public class UpdateProjectCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateProjectCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();

    public UpdateProjectCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();
        var user = AutoFixtures.AppUser;
        var project = AutoFixtures.Project;

        Identity.GetCurrentUser().Returns(user);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(project);
        UnitOfWork.Statuses.GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(AutoFixtures.TaskStatus with { Id = request.DefaultStatusId ?? 5 });

        var result = await Handler.Handle(new UpdateProjectCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.Description.Should().Be(request.Description);
        result.Payload!.RepositoryUrl.Should().Be(request.RepositoryUrl);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();
        var project = AutoFixtures.Project;

        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        Identity.GetWorkspaceKey().Returns("workspace");
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(project);
        UnitOfWork.Statuses.GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(AutoFixtures.TaskStatus with { Id = request.DefaultStatusId ?? 5 });

        await Handler.Handle(new UpdateProjectCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(message =>
            message.Operation == SearchIndexOperation.Index
            && message.EntityType == "project"
            && message.EntityIds.Contains(project.Id)
            && message.WorkspaceSlug == "workspace"));
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();

        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateProjectCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }
}
