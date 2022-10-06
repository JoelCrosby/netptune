using System.Threading.Tasks;

using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class TaskServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly TaskService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public TaskServiceTests()
    {
        Service = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(default, default)
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int?>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(AutoFixtures.TaskViewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var result = await Service.Create(request);

        result.Should().NotBeNull();
    }
}
