using FluentAssertions;

using Netptune.Core.Enums;

using Xunit;

namespace Netptune.Automation.Tests;

[Collection("automation-database")]
public sealed class AutomationRepositoryQueryTests
{
    private readonly AutomationTestFixture Fixture;

    public AutomationRepositoryQueryTests(AutomationTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task GetSprintAutomationTasks_returns_tasks_for_every_requested_sprint()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var secondProject = await AutomationTestData.CreateProject(scope.Db, scenario, "SECOND");
        var firstSprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "First Sprint");
        var secondSprint = await AutomationTestData.CreateSprint(
            scope.Db,
            scenario,
            "Second Sprint",
            secondProject.Id);
        var secondSprintTask = await AutomationTestData.CreateTask(
            scope.Db,
            scenario,
            "Second Sprint Task",
            secondProject.Id);
        var unscheduledTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Unscheduled Task");

        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, firstSprint.Id);
        await AutomationTestData.AssignTaskToSprint(scope.Db, secondSprintTask.Id, secondSprint.Id);

        var tasks = await scope.UnitOfWork.Tasks.GetSprintAutomationTasks(
            [firstSprint.Id, secondSprint.Id],
            TestContext.Current.CancellationToken);

        tasks.Select(task => task.Id).Should().BeEquivalentTo([scenario.Task.Id, secondSprintTask.Id]);
        tasks.Should().NotContain(task => task.Id == unscheduledTask.Id);
    }

    [Fact]
    public async Task GetSprintAutomationTasks_excludes_deleted_tasks_and_other_sprints()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var otherProject = await AutomationTestData.CreateProject(scope.Db, scenario, "OTHER");
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint");
        var otherSprint = await AutomationTestData.CreateSprint(
            scope.Db,
            scenario,
            "Other Sprint",
            otherProject.Id);
        var deletedTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Deleted Task");
        var otherSprintTask = await AutomationTestData.CreateTask(
            scope.Db,
            scenario,
            "Other Sprint Task",
            otherProject.Id);

        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);
        await AutomationTestData.AssignTaskToSprint(scope.Db, deletedTask.Id, sprint.Id);
        await AutomationTestData.AssignTaskToSprint(scope.Db, otherSprintTask.Id, otherSprint.Id);
        await AutomationTestData.SoftDeleteTask(scope.Db, deletedTask.Id);

        var tasks = await scope.UnitOfWork.Tasks.GetSprintAutomationTasks(
            [sprint.Id],
            TestContext.Current.CancellationToken);

        tasks.Select(task => task.Id).Should().BeEquivalentTo([scenario.Task.Id]);
    }

    [Fact]
    public async Task GetSprintAutomationTasks_returns_nothing_without_sprint_ids()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint");

        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);

        var tasks = await scope.UnitOfWork.Tasks.GetSprintAutomationTasks(
            [],
            TestContext.Current.CancellationToken);

        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSprintAutomationTasks_loads_the_relationships_conditions_rely_on()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint");

        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);

        var tasks = await scope.UnitOfWork.Tasks.GetSprintAutomationTasks(
            [sprint.Id],
            TestContext.Current.CancellationToken);

        var task = tasks.Single();

        task.Status.Should().NotBeNull();
        task.Project.Should().NotBeNull();
        task.Workspace.Should().NotBeNull();
        task.ProjectTaskAppUsers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAutomationTasks_returns_the_requested_tasks_only()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var wanted = await AutomationTestData.CreateTask(scope.Db, scenario, "Wanted Task");
        var deleted = await AutomationTestData.CreateTask(scope.Db, scenario, "Deleted Task");

        await AutomationTestData.SoftDeleteTask(scope.Db, deleted.Id);

        var tasks = await scope.UnitOfWork.Tasks.GetAutomationTasks(
            [wanted.Id, deleted.Id],
            TestContext.Current.CancellationToken);

        tasks.Select(task => task.Id).Should().BeEquivalentTo([wanted.Id]);
    }

    [Fact]
    public async Task GetAutomationTasks_returns_nothing_without_ids()
    {
        await using var scope = await Fixture.CreateScope();

        await AutomationTestData.CreateScenario(scope.Db);

        var tasks = await scope.UnitOfWork.Tasks.GetAutomationTasks(
            [],
            TestContext.Current.CancellationToken);

        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBlockerCounts_counts_incomplete_dependencies_only()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var completedBlocker = await AutomationTestData.CreateTask(scope.Db, scenario, "Completed Blocker");
        var openBlocker = await AutomationTestData.CreateTask(scope.Db, scenario, "Open Blocker");
        var relatedTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Related Task");
        var dependency = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);
        var hierarchy = await AutomationTestData.CreateRelationType(scope.Db, scenario);
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");

        await AutomationTestData.CreateRelation(scope.Db, scenario, dependency, completedBlocker.Id, scenario.Task.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, dependency, openBlocker.Id, scenario.Task.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, hierarchy, relatedTask.Id, scenario.Task.Id);
        await AutomationTestData.SetTaskStatus(scope.Db, completedBlocker.Id, completeStatusId);

        var counts = await scope.UnitOfWork.ProjectTaskRelations.GetBlockerCounts(
            [scenario.Task.Id],
            TestContext.Current.CancellationToken);

        var taskCounts = counts.Single();

        taskCounts.TaskId.Should().Be(scenario.Task.Id);
        taskCounts.Total.Should().Be(2);
        taskCounts.Incomplete.Should().Be(1);
    }

    [Fact]
    public async Task GetBlockerCounts_returns_nothing_for_a_task_without_dependencies()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        var counts = await scope.UnitOfWork.ProjectTaskRelations.GetBlockerCounts(
            [scenario.Task.Id],
            TestContext.Current.CancellationToken);

        counts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChildCounts_counts_incomplete_children_only()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var completedChild = await AutomationTestData.CreateTask(scope.Db, scenario, "Completed Child");
        var openChild = await AutomationTestData.CreateTask(scope.Db, scenario, "Open Child");
        var blockedTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Blocked Task");
        var hierarchy = await AutomationTestData.CreateRelationType(scope.Db, scenario);
        var dependency = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");

        await AutomationTestData.CreateRelation(scope.Db, scenario, hierarchy, scenario.Task.Id, completedChild.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, hierarchy, scenario.Task.Id, openChild.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, dependency, scenario.Task.Id, blockedTask.Id);
        await AutomationTestData.SetTaskStatus(scope.Db, completedChild.Id, completeStatusId);

        var counts = await scope.UnitOfWork.ProjectTaskRelations.GetChildCounts(
            [scenario.Task.Id],
            TestContext.Current.CancellationToken);

        var taskCounts = counts.Single();

        taskCounts.Total.Should().Be(2);
        taskCounts.Incomplete.Should().Be(1);
    }

    [Fact]
    public async Task GetDependentTaskIds_returns_the_tasks_a_blocker_holds_up()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var blockedTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Blocked Task");
        var childTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Child Task");
        var dependency = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);
        var hierarchy = await AutomationTestData.CreateRelationType(scope.Db, scenario);

        await AutomationTestData.CreateRelation(scope.Db, scenario, dependency, scenario.Task.Id, blockedTask.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, hierarchy, scenario.Task.Id, childTask.Id);

        var dependents = await scope.UnitOfWork.ProjectTaskRelations.GetDependentTaskIds(
            [scenario.Task.Id],
            TestContext.Current.CancellationToken);

        dependents.Should().BeEquivalentTo([blockedTask.Id]);
    }

    [Fact]
    public async Task GetParentTaskIds_returns_hierarchy_parents_only()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var parent = await AutomationTestData.CreateTask(scope.Db, scenario, "Parent Task");
        var blocker = await AutomationTestData.CreateTask(scope.Db, scenario, "Blocking Task");
        var hierarchy = await AutomationTestData.CreateRelationType(scope.Db, scenario);
        var dependency = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);

        await AutomationTestData.CreateRelation(scope.Db, scenario, hierarchy, parent.Id, scenario.Task.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, dependency, blocker.Id, scenario.Task.Id);

        var parents = await scope.UnitOfWork.ProjectTaskRelations.GetParentTaskIds(
            [scenario.Task.Id],
            TestContext.Current.CancellationToken);

        parents.Should().BeEquivalentTo([parent.Id]);
    }

    [Fact]
    public async Task GetActiveSprintsEndingBefore_returns_active_sprints_inside_the_window()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var laterProject = await AutomationTestData.CreateProject(scope.Db, scenario, "LATER");
        var endingSprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Ending Sprint");
        var laterSprint = await AutomationTestData.CreateSprint(
            scope.Db,
            scenario,
            "Later Sprint",
            laterProject.Id);
        var completedSprint = await AutomationTestData.CreateSprint(
            scope.Db,
            scenario,
            "Completed Sprint",
            status: SprintStatus.Completed);
        var today = DateTime.UtcNow.Date;

        await AutomationTestData.SetSprintEndDate(scope.Db, endingSprint.Id, today.AddDays(2));
        await AutomationTestData.SetSprintEndDate(scope.Db, laterSprint.Id, today.AddDays(20));
        await AutomationTestData.SetSprintEndDate(scope.Db, completedSprint.Id, today.AddDays(1));

        var sprints = await scope.UnitOfWork.Sprints.GetActiveSprintsEndingBefore(
            [scenario.Workspace.Id],
            today.AddDays(5),
            TestContext.Current.CancellationToken);

        sprints.Select(sprint => sprint.Id).Should().BeEquivalentTo([endingSprint.Id]);
    }

    [Fact]
    public async Task GetActiveSprintsEndingBefore_returns_nothing_without_workspaces()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint");

        var sprints = await scope.UnitOfWork.Sprints.GetActiveSprintsEndingBefore(
            [],
            DateTime.UtcNow.AddDays(30),
            TestContext.Current.CancellationToken);

        sprints.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_maps_status_ids_to_their_category()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var activeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");

        var categories = await scope.UnitOfWork.Statuses.GetCategories(
            [activeStatusId, completeStatusId],
            TestContext.Current.CancellationToken);

        categories[activeStatusId].Should().Be(StatusCategory.Active);
        categories[completeStatusId].Should().Be(StatusCategory.Done);
    }

    [Fact]
    public async Task GetCategories_returns_nothing_without_status_ids()
    {
        await using var scope = await Fixture.CreateScope();

        await AutomationTestData.CreateScenario(scope.Db);

        var categories = await scope.UnitOfWork.Statuses.GetCategories(
            [],
            TestContext.Current.CancellationToken);

        categories.Should().BeEmpty();
    }
}
