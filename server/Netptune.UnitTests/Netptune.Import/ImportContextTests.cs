using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.UnitOfWork;
using Netptune.Import;
using Netptune.Transfer.Mapping;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class ImportContextTests
{
    [Fact]
    public async Task NextScopeId_ShouldHandOutNumbersReservedFromTheProject()
    {
        var unitOfWork = Substitute.For<INetptuneUnitOfWork>();
        var projects = Substitute.For<IProjectRepository>();

        unitOfWork.Projects.Returns(projects);
        projects.ReserveTaskScopeIds(1, 3, Arg.Any<CancellationToken>()).Returns(40);

        var context = Context();

        await context.ReserveScopeIds(unitOfWork, 3, TestContext.Current.CancellationToken);

        context.NextScopeId().Should().Be(40);
        context.NextScopeId().Should().Be(41);
        context.NextScopeId().Should().Be(42);

        await projects.Received(1).ReserveTaskScopeIds(1, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveScopeIds_ShouldNotTouchTheProjectWhenNothingIsBeingCreated()
    {
        var unitOfWork = Substitute.For<INetptuneUnitOfWork>();
        var projects = Substitute.For<IProjectRepository>();

        unitOfWork.Projects.Returns(projects);

        await Context().ReserveScopeIds(unitOfWork, 0, TestContext.Current.CancellationToken);

        await projects.DidNotReceive().ReserveTaskScopeIds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void NextScopeId_ShouldRefuseToRunPastTheReservedBlock()
    {
        var context = Context();
        var take = () => context.NextScopeId();

        take.Should().Throw<InvalidOperationException>("nothing was reserved, so no number is ours to hand out");
    }

    [Fact]
    public void FindExisting_ShouldIgnoreATaskWithTheSameNumberInAnotherProject()
    {
        var mine = new ProjectTask { Id = 1, ProjectId = 1, ProjectScopeId = 14 };
        var theirs = new ProjectTask { Id = 2, ProjectId = 2, ProjectScopeId = 14 };
        var context = Context([theirs, mine]);

        context.FindExisting(Row("acme-14")).Should().BeSameAs(mine);
        context.FindExisting(Row("zulu-14")).Should().BeNull();
    }

    [Fact]
    public void FindExisting_ShouldMatchOnTheIdAnEarlierImportStored()
    {
        var task = new ProjectTask { Id = 3, ProjectId = 1, ProjectScopeId = 9, ExternalId = "PROJ-101" };
        var context = Context([task]);

        context.FindExisting(Row("proj-101")).Should().BeSameAs(task);
        context.FindExisting(Row("PROJ-999")).Should().BeNull();
    }

    [Fact]
    public void FindStatusInCategory_ShouldPickTheFirstStatusInThatCategory()
    {
        var todo = TaskStatus(1, StatusCategory.Todo, sortOrder: 0);
        var review = TaskStatus(2, StatusCategory.Active, sortOrder: 2);
        var doing = TaskStatus(3, StatusCategory.Active, sortOrder: 1);
        var context = StatusContext(defaultStatusId: null, todo, review, doing);

        context.FindStatusInCategory("In Progress").Should().BeSameAs(doing);
    }

    [Fact]
    public void FindStatusInCategory_ShouldPreferTheProjectDefault_WhenItIsInTheCategory()
    {
        var first = TaskStatus(1, StatusCategory.Todo, sortOrder: 0);
        var projectDefault = TaskStatus(2, StatusCategory.Todo, sortOrder: 1);
        var context = StatusContext(defaultStatusId: 2, first, projectDefault);

        context.FindStatusInCategory("To Do").Should().BeSameAs(projectDefault);
    }

    [Fact]
    public void FindStatusInCategory_ShouldReturnNull_WhenTheCategoryIsUnknownOrHasNoStatus()
    {
        var todo = TaskStatus(1, StatusCategory.Todo, sortOrder: 0);
        var context = StatusContext(defaultStatusId: null, todo);

        context.FindStatusInCategory("Waiting").Should().BeNull();
        context.FindStatusInCategory("Done").Should().BeNull();
        context.FindStatusInCategory(null).Should().BeNull();
    }

    [Fact]
    public void FindStatusInCategory_ShouldIgnoreStatusesThatAreNotForTasks()
    {
        var todo = TaskStatus(1, StatusCategory.Todo, sortOrder: 0);
        var pageDone = new Status
        {
            Id = 2,
            Key = "s2",
            Name = "S2",
            Category = StatusCategory.Done,
            EntityType = EntityType.Page,
        };
        var context = StatusContext(defaultStatusId: null, todo, pageDone);

        context.FindStatusInCategory("Done").Should().BeNull();
    }

    private static ResolvedTaskRow Row(string sourceId)
    {
        return new ResolvedTaskRow { RowNumber = 1, SourceId = sourceId };
    }

    private static ImportContext Context(IReadOnlyList<ProjectTask>? existingTasks = null)
    {
        var project = new Project { Id = 1, Key = "acme", NextTaskScopeId = 100 };
        var board = new Board { Id = 1, ProjectId = 1 };
        var status = new Status { Id = 1, Key = "todo", Name = "Todo", Category = StatusCategory.Todo, EntityType = EntityType.Task };

        return new ImportContext(project, board, Vocabulary(), [status], [], [], existingTasks ?? []);
    }

    private static Status TaskStatus(int id, StatusCategory category, int sortOrder)
    {
        return new Status
        {
            Id = id,
            Key = $"s{id}",
            Name = $"S{id}",
            Category = category,
            EntityType = EntityType.Task,
            SortOrder = sortOrder,
        };
    }

    private static ImportContext StatusContext(int? defaultStatusId, params Status[] statuses)
    {
        var project = new Project { Id = 1, Key = "acme", DefaultStatusId = defaultStatusId };
        var board = new Board { Id = 1, ProjectId = 1 };

        return new ImportContext(project, board, Vocabulary(), statuses, [], [], []);
    }

    private static ImportVocabulary Vocabulary()
    {
        return new ImportVocabulary
        {
            StatusesByKey = [],
            StatusesByName = [],
            TagsByName = [],
            UsersByEmail = [],
            BoardGroupsByName = [],
            SprintsByName = [],
        };
    }
}
