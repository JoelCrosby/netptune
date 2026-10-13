using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Sprints.Queries;

public class GetSprintsByIdQueryHandlerTests
{
    private const int WorkspaceId = 1;

    private readonly GetSprintsByIdQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetSprintsByIdQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);

        Identity.GetWorkspaceId().Returns(WorkspaceId);
    }

    [Fact]
    public async Task Handle_ShouldLeaveOutSprintsFromOtherWorkspaces()
    {
        UnitOfWork.Sprints
            .GetSprintViewModels(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([CreateSprint(4, WorkspaceId), CreateSprint(5, 2)]);

        var query = new GetSprintsByIdQuery([4, 5]);
        var result = await Handler.Handle(query, TestContext.Current.CancellationToken);

        result.Select(sprint => sprint.Id).Should().Equal(4);
    }

    private static SprintViewModel CreateSprint(int id, int workspaceId)
    {
        return new SprintViewModel
        {
            Id = id,
            Name = $"Sprint {id}",
            Status = SprintStatus.Active,
            ProjectId = 3,
            ProjectName = "Netptune",
            ProjectKey = "NPT",
            WorkspaceId = workspaceId,
        };
    }
}
