using AutoFixture;

using FluentAssertions;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Queries;

public class GetArchivedTasksQueryHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly GetArchivedTasksQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetArchivedTasksQueryHandlerTests()
    {
        Fixture.Register(() => DateOnly.FromDateTime(Fixture.Create<DateTime>()));
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetArchivedTasks_ShouldQueryDeletedTasks_WhenInputValid()
    {
        var tasks = Fixture.Create<List<TaskViewModel>>();
        var page = new PagedResponse<TaskViewModel>(tasks, 1, 50, tasks.Count);
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTasksAsync(workspaceKey, null, true, true, TestContext.Current.CancellationToken).Returns(page);

        var result = await Handler.Handle(new GetArchivedTasksQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().BeEquivalentTo(page);

        await UnitOfWork.Tasks.Received(1).GetTasksAsync(workspaceKey, null, true, true, TestContext.Current.CancellationToken);
    }
}
