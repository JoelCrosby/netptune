using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiChangeSetMapperTests
{
    private readonly IAiUndoCatalog UndoCatalog = Substitute.For<IAiUndoCatalog>();

    private readonly AiChangeRouteIds RouteIds = new()
    {
        Tasks = new Dictionary<int, string> { [1] = "NETP-1" },
        Projects = new Dictionary<int, string> { [2] = "WEB" },
        Boards = new Dictionary<int, string> { [3] = "delivery" },
        Sprints = new Dictionary<int, string> { [4] = "sprint-4" },
    };

    [Fact]
    public void ToViewModel_ShouldRouteEachEntityBySlug()
    {
        var changes = new List<AiProposedChange>
        {
            Change(1, "task", entityId: 1),
            Change(2, "project", appliedEntityId: 2),
            Change(3, "board", entityId: 3),
            Change(4, "sprint", entityId: 4),
        };

        var model = AiChangeSetMapper.ToViewModel(ChangeSet(), changes, RouteIds, UndoCatalog);
        var routeIds = model.Changes.Select(change => change.EntityRouteId);

        routeIds.Should().Equal("NETP-1", "WEB", "delivery", "sprint-4");
    }

    [Fact]
    public void ToViewModel_ShouldOnlyGiveTasksASystemId()
    {
        var changes = new List<AiProposedChange>
        {
            Change(1, "task", entityId: 1),
            Change(2, "project", entityId: 2),
        };

        var model = AiChangeSetMapper.ToViewModel(ChangeSet(), changes, RouteIds, UndoCatalog);

        model.Changes.Select(change => change.EntitySystemId).Should().Equal("NETP-1", null);
    }

    private static AiChangeSet ChangeSet()
    {
        return new AiChangeSet { Id = Guid.NewGuid(), UserId = "user" };
    }

    private static AiProposedChange Change(int sequence, string entityType, int? entityId = null, int? appliedEntityId = null)
    {
        return new AiProposedChange
        {
            Id = sequence,
            Sequence = sequence,
            ToolName = "propose_change",
            EntityType = entityType,
            EntityId = entityId,
            AppliedEntityId = appliedEntityId,
            Summary = "Change",
        };
    }
}
