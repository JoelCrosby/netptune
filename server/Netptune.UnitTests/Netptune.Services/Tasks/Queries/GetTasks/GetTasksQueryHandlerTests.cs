using AutoFixture;

using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Queries;

public class GetTasksQueryHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly GetTasksQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTasksQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var tasks = Fixture.Create<List<TaskViewModel>>();
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTasksAsync(workspaceKey).Returns(tasks);

        var result = await Handler.Handle(new GetTasksQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(tasks);
    }
}
