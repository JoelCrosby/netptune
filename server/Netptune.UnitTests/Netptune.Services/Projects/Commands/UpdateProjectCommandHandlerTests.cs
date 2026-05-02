using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Projects.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Projects.Commands;

public class UpdateProjectCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateProjectCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateProjectCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();
        var user = AutoFixtures.AppUser;
        var project = AutoFixtures.Project;

        Identity.GetCurrentUser().Returns(user);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(project);

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

        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(AutoFixtures.Project);

        await Handler.Handle(new UpdateProjectCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
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
