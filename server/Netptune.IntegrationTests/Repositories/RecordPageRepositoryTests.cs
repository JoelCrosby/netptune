using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Requests;
using Netptune.Core.UnitOfWork;

using Xunit;

namespace Netptune.IntegrationTests.Repositories;

public sealed class RecordPageRepositoryTests
{
    private const string WorkspaceKey = "netptune";

    private readonly NetptuneFixture Fixture;

    public RecordPageRepositoryTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task GetProjectsPage_ShouldPage_AndNarrowBySearch()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var firstFilter = new ProjectFilter { PageSize = 1 };
        var all = await unitOfWork.Projects.GetProjectsPage(WorkspaceKey, firstFilter, cancellationToken);
        var name = all.Items.Single().Name;
        var searchFilter = new ProjectFilter { Search = name };
        var searched = await unitOfWork.Projects.GetProjectsPage(WorkspaceKey, searchFilter, cancellationToken);

        all.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        searched.Items.Should().Contain(project => project.Name == name);
    }

    [Fact]
    public async Task GetBoardsPage_ShouldOrderByName_WhenAskedTo()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var filter = new BoardFilter { SortBy = "name", PageSize = 100 };

        var page = await unitOfWork.Boards.GetBoardsPage(WorkspaceKey, filter, TestContext.Current.CancellationToken);
        var names = page.Items.Select(board => board.Name.ToLowerInvariant()).ToList();

        names.Should().HaveCountGreaterThan(1);
        names.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetOptionsPage_ShouldPage_AndNarrowToOneBoard()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var workspaceId = await unitOfWork.Workspaces.GetIdBySlug(WorkspaceKey, cancellationToken);

        var firstFilter = new BoardGroupOptionFilter { PageSize = 1 };
        var first = await unitOfWork.BoardGroups.GetOptionsPage(workspaceId!.Value, firstFilter, cancellationToken);
        var boardId = first.Items.Single().BoardId;
        var boardFilter = new BoardGroupOptionFilter { BoardId = boardId, PageSize = 100 };
        var onBoard = await unitOfWork.BoardGroups.GetOptionsPage(workspaceId.Value, boardFilter, cancellationToken);

        first.TotalCount.Should().BeGreaterThan(1);
        onBoard.Items.Should().NotBeEmpty().And.OnlyContain(option => option.BoardId == boardId);
        onBoard.TotalCount.Should().BeLessThanOrEqualTo(first.TotalCount);
    }

    [Fact]
    public async Task GetSprintsPage_ShouldPage_AndCountEverySprint()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var pageFilter = new SprintPageFilter { PageSize = 1 };
        var page = await unitOfWork.Sprints.GetSprintsPage(WorkspaceKey, pageFilter, cancellationToken);
        var missingFilter = new SprintPageFilter { Search = $"no-sprint-{Guid.NewGuid():N}" };
        var missing = await unitOfWork.Sprints.GetSprintsPage(WorkspaceKey, missingFilter, cancellationToken);

        page.Items.Count.Should().BeLessThanOrEqualTo(1);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(page.Items.Count);
        missing.TotalCount.Should().Be(0);
    }
}
