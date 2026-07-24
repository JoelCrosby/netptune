using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Automation.Execution;
using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Meta;
using Netptune.Core.Models.Automations;
using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.Automation.Tests;

public sealed class AutomationExecutionServiceTests
{
    private readonly AutomationTestFixture Fixture;

    public AutomationExecutionServiceTests(AutomationTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_creates_run_and_flag_for_matching_status_rule()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        var newStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "new");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var conditionGroup = CreateStatusConditionGroup(inProgressStatusId);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, newStatusId, inProgressStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        var flag = await scope.Db.Flags.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
        run.EntityType.Should().Be(EntityType.Task);
        run.EntityId.Should().Be(scenario.Task.Id);

        flag.AutomationRuleId.Should().Be(rule.Id);
        flag.EntityType.Should().Be(EntityType.Task);
        flag.EntityId.Should().Be(scenario.Task.Id);
        flag.Name.Should().Be("Needs attention");
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_can_flag_task_again_after_previous_flag_is_resolved()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        var newStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "new");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var conditionGroup = CreateStatusConditionGroup(inProgressStatusId);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup);

        TaskChangedMessage CreateMessage() => new()
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, newStatusId, inProgressStatusId),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(
            CreateMessage(),
            TestContext.Current.CancellationToken);

        var firstFlag = await scope.Db.Flags.SingleAsync(TestContext.Current.CancellationToken);
        firstFlag.IsDeleted = true;
        firstFlag.Resolution = FlagResolutionType.Resolved;
        firstFlag.ResolvedAt = DateTime.UtcNow;
        firstFlag.ResolvedByUserId = scenario.Owner.Id;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteTaskChangedRules(
            CreateMessage(),
            TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var flags = await scope.Db.Flags
            .IgnoreQueryFilters()
            .Where(flag => flag.EntityId == scenario.Task.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        flags.Should().HaveCount(2);
        flags.Should().ContainSingle(flag => !flag.IsDeleted);
        flags.Should().ContainSingle(flag => flag.Resolution == FlagResolutionType.Resolved);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_fails_without_mutation_when_permission_is_revoked()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        var newStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "new");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var conditionGroup = CreateStatusConditionGroup(inProgressStatusId);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup);

        var membership = await scope.Db.WorkspaceAppUsers.SingleAsync(
            item => item.UserId == scenario.ExecutionUser.Id,
            TestContext.Current.CancellationToken);
        membership.Permissions = [];
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, newStatusId, inProgressStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var run = await scope.Db.AutomationRuns
            .Include(item => item.ActionResults)
            .SingleAsync(TestContext.Current.CancellationToken);

        run.Status.Should().Be(AutomationRunStatus.Failed);
        run.Message.Should().Contain("required permissions");
        run.ActionResults.Should().ContainSingle()
            .Which.Status.Should().Be(AutomationActionResultStatus.Skipped);
        (await scope.Db.Flags.AnyAsync(TestContext.Current.CancellationToken))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_is_idempotent_for_same_event()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var conditionGroup = CreateStatusConditionGroup(completeStatusId);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup);

        var eventId = Guid.NewGuid();
        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = eventId,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
        flagCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_creates_run_for_matching_name_rule()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskChangedRule(scope.Db, scenario, [TaskChangeField.Name]);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.TriggerType.Should().Be(AutomationTriggerType.TaskChanged);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_updates_task_for_matching_rule()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.UpdateTask);
        var sourceEventId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var configuredAction = await scope.Db.AutomationActions
            .Where(action => action.Type == AutomationActionType.UpdateTask)
            .Select(action => new
            {
                action.Id,
                StatusId = action.Config!.RootElement.GetProperty("statusId").GetInt32(),
            })
            .SingleAsync(TestContext.Current.CancellationToken);
        var expectedStatusId = await scope.Db.Statuses
            .Where(status => status.WorkspaceId == scenario.Workspace.Id && status.Key == "complete")
            .Select(status => status.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        configuredAction.StatusId.Should().Be(expectedStatusId);
        scope.Db.ChangeTracker.Clear();

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            EventId = sourceEventId,
            CorrelationId = correlationId,
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        run.Status.Should().Be(AutomationRunStatus.Succeeded, run.Message);

        var task = await scope.Db.ProjectTasks
            .Include(task => task.Status)
            .SingleAsync(TestContext.Current.CancellationToken);

        task.Priority.Should().Be(TaskPriority.High);
        task.StatusId.Should().Be(expectedStatusId);
        task.Status.Key.Should().Be("complete");
        task.ModifiedByUserId.Should().Be(scenario.ExecutionUser.Id);

        var fieldEvents = scope.EventRecords.Events
            .Where(recordedEvent => recordedEvent.EventKey == EventKeys.EntityFieldTransitioned)
            .ToList();
        fieldEvents.Should().HaveCount(2);
        fieldEvents.Should().OnlyContain(recordedEvent =>
            recordedEvent.ActorUserId == scenario.ExecutionUser.Id);
        fieldEvents.Should().OnlyContain(recordedEvent => recordedEvent.CorrelationId == correlationId);
        fieldEvents.Should().OnlyContain(recordedEvent => recordedEvent.CausationEventId == sourceEventId);
        var transitionPayloads = fieldEvents
            .Select(recordedEvent => (FieldTransitionedPayload)recordedEvent.Payload)
            .ToList();
        transitionPayloads.Should().OnlyContain(payload => payload.OriginType == EventOriginType.Automation);
        transitionPayloads.Should().OnlyContain(payload => payload.AutomationRuleId == rule.Id);
        transitionPayloads.Should().OnlyContain(payload => payload.AutomationRunId == run.Id);
        transitionPayloads.Should().OnlyContain(payload => payload.ChainDepth == 1);
        transitionPayloads
            .Select(payload => payload.Field)
            .Should()
            .BeEquivalentTo("status", "priority");

        var taskChanged = scope.EventPublisher.Events.OfType<TaskChangedMessage>().Should().ContainSingle().Subject;
        taskChanged.ActorUserId.Should().Be(scenario.ExecutionUser.Id);
        taskChanged.TaskId.Should().Be(scenario.Task.Id);
        taskChanged.OriginType.Should().Be(EventOriginType.Automation);
        taskChanged.CorrelationId.Should().Be(correlationId);
        taskChanged.CausationEventId.Should().Be(sourceEventId);
        taskChanged.AutomationRuleId.Should().Be(rule.Id);
        taskChanged.AutomationRunId.Should().Be(run.Id);
        taskChanged.ChainDepth.Should().Be(1);
        taskChanged.Changes
            .Select(change => change.Field)
            .Should()
            .BeEquivalentTo([TaskChangeField.Status, TaskChangeField.Priority]);

        var runHistory = await scope.UnitOfWork.Automations.GetRuns(
            rule.Id,
            scenario.Workspace.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        var actionResult = runHistory.Should().ContainSingle().Subject.ActionResults.Should().ContainSingle().Subject;

        actionResult.AutomationActionId.Should().Be(configuredAction.Id);
        actionResult.ActionType.Should().Be(AutomationActionType.UpdateTask);
        actionResult.Status.Should().Be(AutomationActionResultStatus.Succeeded);
        actionResult.StartedAt.Should().NotBeNull();
        actionResult.CompletedAt.Should().NotBeNull();
        actionResult.Output.Should().NotBeNull();

        run.Context!.RootElement.GetProperty("initiatingUserId").GetString()
            .Should().Be(scenario.Owner.Id);
        run.Context.RootElement.GetProperty("executionUserId").GetString()
            .Should().Be(scenario.ExecutionUser.Id);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_applies_expanded_task_updates()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.UpdateTask);
        var nextOwner = new AppUser
        {
            Id = "next-owner",
            Firstname = "Next",
            Lastname = "Owner",
            UserName = "next-owner@example.test",
            NormalizedUserName = "NEXT-OWNER@EXAMPLE.TEST",
            Email = "next-owner@example.test",
            NormalizedEmail = "NEXT-OWNER@EXAMPLE.TEST",
            EmailConfirmed = true,
        };
        var nextOwnerMembership = new WorkspaceAppUser
        {
            User = nextOwner,
            Workspace = scenario.Workspace,
            Role = WorkspaceRole.Member,
            Permissions = [NetptunePermissions.Tasks.Read],
        };
        var oldTag = new Tag
        {
            Name = "old-tag",
            Workspace = scenario.Workspace,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };
        var newTag = new Tag
        {
            Name = "new-tag",
            Workspace = scenario.Workspace,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };
        var sprint = new Sprint
        {
            Name = "Target sprint",
            Workspace = scenario.Workspace,
            Project = scenario.Project,
            Status = SprintStatus.Planning,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };
        var board = new Board
        {
            Name = "Automation board",
            Identifier = "automation-board",
            Workspace = scenario.Workspace,
            Project = scenario.Project,
            BoardType = BoardType.Default,
            MetaInfo = new BoardMeta(),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };
        var boardGroup = new BoardGroup
        {
            Name = "Doing",
            Workspace = scenario.Workspace,
            Board = board,
            SortOrder = 1,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        scenario.Task.ProjectTaskTags.Add(new ProjectTaskTag { Tag = oldTag });
        scope.Db.AddRange(nextOwner, nextOwnerMembership, oldTag, newTag, sprint, board, boardGroup);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var action = await scope.Db.AutomationActions
            .SingleAsync(
                item => item.AutomationRuleId == rule.Id && !item.IsDeleted,
                TestContext.Current.CancellationToken);
        action.Config = JsonSerializer.SerializeToDocument(new
        {
            name = "Updated by automation",
            description = "Expanded update",
            ownerId = nextOwner.Id,
            assigneeIds = Array.Empty<string>(),
            addTags = new[] { newTag.Name },
            removeTags = new[] { oldTag.Name },
            dueDate = new
            {
                mode = AutomationDateUpdateMode.RelativeBusinessDays,
                offset = 3,
            },
            estimateType = EstimateType.Hours,
            estimateValue = 8,
            sprintId = sprint.Id,
            boardGroupId = boardGroup.Id,
        }, JsonOptions.Default);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        scope.Db.ChangeTracker.Clear();

        var runDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            EventId = Guid.NewGuid(),
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var task = await scope.Db.ProjectTasks
            .Include(item => item.ProjectTaskAppUsers)
            .Include(item => item.ProjectTaskTags)
            .ThenInclude(item => item.Tag)
            .SingleAsync(item => item.Id == scenario.Task.Id, TestContext.Current.CancellationToken);
        var taskInGroup = await scope.Db.ProjectTaskInBoardGroups
            .SingleAsync(item => item.ProjectTaskId == task.Id, TestContext.Current.CancellationToken);

        task.Name.Should().Be("Updated by automation");
        task.Description.Should().Be("Expanded update");
        task.OwnerId.Should().Be(nextOwner.Id);
        task.ProjectTaskAppUsers.Should().BeEmpty();
        task.ProjectTaskTags.Select(item => item.Tag.Name).Should().Equal("new-tag");
        task.DueDate.Should().Be(AddBusinessDays(runDate, 3));
        task.EstimateType.Should().Be(EstimateType.Hours);
        task.EstimateValue.Should().Be(8);
        task.SprintId.Should().Be(sprint.Id);
        taskInGroup.BoardGroupId.Should().Be(boardGroup.Id);

        var taskChanged = scope.EventPublisher.Events
            .OfType<TaskChangedMessage>()
            .Should()
            .ContainSingle()
            .Subject;
        taskChanged.Changes.Select(change => change.Field).Should().Contain(
        [
            TaskChangeField.Name,
            TaskChangeField.Description,
            TaskChangeField.Owner,
            TaskChangeField.Assignees,
            TaskChangeField.Tags,
            TaskChangeField.DueDate,
            TaskChangeField.Estimate,
            TaskChangeField.Sprint,
            TaskChangeField.BoardGroup,
        ]);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_clears_nullable_task_fields()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.UpdateTask);
        var sprint = new Sprint
        {
            Name = "Current sprint",
            Workspace = scenario.Workspace,
            Project = scenario.Project,
            Status = SprintStatus.Planning,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        scenario.Task.Description = "Remove me";
        scenario.Task.EstimateType = EstimateType.StoryPoints;
        scenario.Task.EstimateValue = 5;
        scenario.Task.StartDate = new DateOnly(2026, 7, 20);
        scenario.Task.DueDate = new DateOnly(2026, 7, 30);
        scenario.Task.Sprint = sprint;
        scope.Db.Sprints.Add(sprint);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var action = await scope.Db.AutomationActions
            .SingleAsync(
                item => item.AutomationRuleId == rule.Id && !item.IsDeleted,
                TestContext.Current.CancellationToken);
        action.Config = JsonSerializer.SerializeToDocument(new
        {
            clearDescription = true,
            clearOwner = true,
            clearEstimate = true,
            clearSprint = true,
            startDate = new { mode = AutomationDateUpdateMode.Clear },
            dueDate = new { mode = AutomationDateUpdateMode.Clear },
        }, JsonOptions.Default);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        scope.Db.ChangeTracker.Clear();

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            EventId = Guid.NewGuid(),
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var task = await scope.Db.ProjectTasks
            .SingleAsync(item => item.Id == scenario.Task.Id, TestContext.Current.CancellationToken);
        var taskView = await scope.UnitOfWork.Tasks.GetTaskViewModel(
            task.Id,
            TestContext.Current.CancellationToken);

        task.Description.Should().BeNull();
        task.OwnerId.Should().BeNull();
        task.EstimateType.Should().BeNull();
        task.EstimateValue.Should().BeNull();
        task.StartDate.Should().BeNull();
        task.DueDate.Should().BeNull();
        task.SprintId.Should().BeNull();
        taskView.Should().NotBeNull();
        taskView!.OwnerId.Should().BeNull();
        taskView.OwnerUsername.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_skips_source_rule_but_allows_other_rules_in_chain()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sourceRule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name]);
        var chainedRule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name]);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            OriginType = EventOriginType.Automation,
            AutomationRuleId = sourceRule.Id,
            ChainDepth = 1,
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().ContainSingle();
        runs[0].AutomationRuleId.Should().Be(chainedRule.Id);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_stops_when_chain_depth_limit_is_reached()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name]);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            OriginType = EventOriginType.Automation,
            ChainDepth = AutomationChainPolicy.MaxDepth,
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(0);
        flagCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_adds_comment_for_matching_rule()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.AddComment);

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var comment = await scope.Db.Comments.SingleAsync(TestContext.Current.CancellationToken);
        var commentEvent = scope.EventRecords.Events.Single(record => record.EventKey == EventKeys.CommentCreated);
        var payload = commentEvent.Payload.Should().BeOfType<CommentEventPayload>().Subject;

        comment.Body.Should().Be("Added by test automation");
        comment.EntityType.Should().Be(EntityType.Task);
        comment.EntityId.Should().Be(scenario.Task.Id);
        comment.WorkspaceId.Should().Be(scenario.Workspace.Id);
        comment.OwnerId.Should().Be(scenario.ExecutionUser.Id);
        commentEvent.SubjectType.Should().Be(EventEntityTypes.From(EntityType.Task));
        commentEvent.SubjectId.Should().Be(scenario.Task.Id.ToString());
        commentEvent.ActorUserId.Should().Be(scenario.ExecutionUser.Id);
        payload.CommentId.Should().Be(comment.Id);
        payload.RecipientUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_deletes_task_for_matching_rule()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var conditionGroup = CreateStatusConditionGroup(completeStatusId);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup,
            actionType: AutomationActionType.DeleteTask);

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var task = await scope.Db.ProjectTasks.IgnoreQueryFilters().SingleAsync(task => task.Id == scenario.Task.Id, TestContext.Current.CancellationToken);
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        task.IsDeleted.Should().BeTrue();
        task.DeletedByUserId.Should().Be(scenario.ExecutionUser.Id);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Theory]
    [InlineData(true, "new")]
    [InlineData(false, "complete")]
    public async Task ExecuteTaskChangedRules_applies_cross_type_actions_in_sort_order(
        bool deleteFirst,
        string expectedStatusKey)
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name]);
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var existingActions = rule.Actions.ToList();
        var deleteSortOrder = deleteFirst ? 1 : 2;
        var updateSortOrder = deleteFirst ? 2 : 1;

        scope.Db.AutomationActions.RemoveRange(existingActions);
        rule.Actions.Clear();
        rule.Actions.Add(new AutomationAction
        {
            Type = AutomationActionType.DeleteTask,
            SortOrder = deleteSortOrder,
            Config = JsonSerializer.SerializeToDocument(new { }),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        });
        rule.Actions.Add(new AutomationAction
        {
            Type = AutomationActionType.UpdateTask,
            SortOrder = updateSortOrder,
            Config = JsonSerializer.SerializeToDocument(new
            {
                statusId = completeStatusId,
                priority = TaskPriority.High,
            }),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        });

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        scope.Db.ChangeTracker.Clear();

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var task = await scope.Db.ProjectTasks
            .IgnoreQueryFilters()
            .Include(item => item.Status)
            .SingleAsync(item => item.Id == scenario.Task.Id, TestContext.Current.CancellationToken);

        task.IsDeleted.Should().BeTrue();
        task.Status.Key.Should().Be(expectedStatusKey);

        var actionResults = await scope.Db.AutomationActionResults
            .OrderBy(result => result.SortOrder)
            .ToListAsync(TestContext.Current.CancellationToken);
        var expectedActionTypes = deleteFirst
            ? new List<AutomationActionType>
            {
                AutomationActionType.DeleteTask,
                AutomationActionType.UpdateTask,
            }
            : new List<AutomationActionType>
            {
                AutomationActionType.UpdateTask,
                AutomationActionType.DeleteTask,
            };
        var expectedSecondStatus = deleteFirst
            ? AutomationActionResultStatus.Skipped
            : AutomationActionResultStatus.Succeeded;

        actionResults.Select(result => result.ActionType).Should().Equal(expectedActionTypes);
        actionResults[0].Status.Should().Be(AutomationActionResultStatus.Succeeded);
        actionResults[1].Status.Should().Be(expectedSecondStatus);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_discards_prior_actions_when_planning_fails()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name]);
        var existingActions = rule.Actions.ToList();

        scope.Db.AutomationActions.RemoveRange(existingActions);
        rule.Actions.Clear();
        rule.Actions.Add(new AutomationAction
        {
            Type = AutomationActionType.AddComment,
            SortOrder = 1,
            Config = JsonSerializer.SerializeToDocument(new
            {
                comment = "This action must be discarded",
            }),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        });
        rule.Actions.Add(new AutomationAction
        {
            Type = AutomationActionType.DeleteTask,
            SortOrder = 2,
            Config = JsonSerializer.SerializeToDocument(new
            {
                delayAmount = int.MaxValue,
                delayUnit = AutomationDelayUnit.Days,
            }),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        });

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        scope.Db.ChangeTracker.Clear();

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        var commentCount = await scope.Db.Comments.CountAsync(TestContext.Current.CancellationToken);
        var actionResults = await scope.Db.AutomationActionResults
            .OrderBy(result => result.SortOrder)
            .ToListAsync(TestContext.Current.CancellationToken);

        run.Status.Should().Be(AutomationRunStatus.Failed);
        run.Message.Should().NotBeNullOrWhiteSpace();
        commentCount.Should().Be(0);
        actionResults.Select(result => result.Status).Should().Equal(
            AutomationActionResultStatus.Skipped,
            AutomationActionResultStatus.Failed);
        actionResults[0].Message.Should().Contain("cancelled");
        actionResults[1].Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteScheduledActions_deletes_task_after_configured_delay()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var conditionGroup = CreateStatusConditionGroup(completeStatusId);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup,
            actionType: AutomationActionType.DeleteTask);
        await ConfigureDeleteDelay(scope.Db, rule, 1, AutomationDelayUnit.Hours);

        var triggeredAt = DateTime.UtcNow;

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            OccurredAt = triggeredAt,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(TestContext.Current.CancellationToken);
        var actionResult = await scope.Db.AutomationActionResults.SingleAsync(TestContext.Current.CancellationToken);
        var taskBeforeDelay = await scope.Db.ProjectTasks.SingleAsync(TestContext.Current.CancellationToken);

        taskBeforeDelay.IsDeleted.Should().BeFalse();
        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Pending);
        actionResult.Status.Should().Be(AutomationActionResultStatus.Scheduled);
        scheduledAction.ExpectedStatusId.Should().Be(completeStatusId);
        scheduledAction.ExecuteAt.Should().BeCloseTo(triggeredAt.AddHours(1), TimeSpan.FromMilliseconds(1));

        scheduledAction.ExecuteAt = DateTime.UtcNow.AddMinutes(-1);

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var deletedTask = await scope.Db.ProjectTasks.IgnoreQueryFilters()
            .SingleAsync(TestContext.Current.CancellationToken);

        var completedAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);

        deletedTask.IsDeleted.Should().BeTrue();
        deletedTask.DeletedByUserId.Should().Be(scenario.ExecutionUser.Id);
        completedAction.Status.Should().Be(ScheduledAutomationActionStatus.Completed);
        completedAction.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_cancels_delayed_deletion_when_status_changes()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var conditionGroup = CreateStatusConditionGroup(completeStatusId);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            conditionGroup,
            actionType: AutomationActionType.DeleteTask);

        await ConfigureDeleteDelay(scope.Db, rule, 1, AutomationDelayUnit.Hours);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        await scope.Db.ProjectTasks
            .Where(task => task.Id == scenario.Task.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(task => task.StatusId, inProgressStatusId),
                TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, completeStatusId, inProgressStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(TestContext.Current.CancellationToken);

        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Cancelled);

        scheduledAction.ExecuteAt = DateTime.UtcNow.AddMinutes(-1);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var task = await scope.Db.ProjectTasks.SingleAsync(TestContext.Current.CancellationToken);

        task.IsDeleted.Should().BeFalse();
        task.StatusId.Should().Be(inProgressStatusId);
    }

    [Fact]
    public async Task ExecuteScheduledActions_claims_due_action_once_across_competing_workers()
    {
        await using var scope = await Fixture.CreateScope();
        var setup = await ScheduleDueTaskDeletion(scope);
        await using var competingScope = Fixture.CreateAdditionalScope();

        await Task.WhenAll(
            scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken),
            competingScope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken));

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);
        var deletedTask = await scope.Db.ProjectTasks
            .IgnoreQueryFilters()
            .SingleAsync(task => task.Id == setup.Scenario.Task.Id, TestContext.Current.CancellationToken);

        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Completed);
        scheduledAction.AttemptCount.Should().Be(1);
        scheduledAction.ClaimId.Should().BeNull();
        scheduledAction.LeaseExpiresAt.Should().BeNull();
        deletedTask.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteScheduledActions_fails_immediately_without_a_valid_execution_principal()
    {
        await using var scope = await Fixture.CreateScope();
        var setup = await ScheduleDueTaskDeletion(scope);

        setup.ScheduledAction.OwnerId = null;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await scope.Db.ScheduledAutomationActions
            .Where(action => action.Id == setup.ScheduledAction.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(action => action.ExecuteAt, DateTime.UtcNow.AddMinutes(-1)),
                TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);
        var task = await scope.Db.ProjectTasks.SingleAsync(
            task => task.Id == setup.Scenario.Task.Id,
            TestContext.Current.CancellationToken);

        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Failed);
        scheduledAction.AttemptCount.Should().Be(1);
        scheduledAction.ProcessedAt.Should().NotBeNull();
        scheduledAction.LastError.Should().Contain("no valid execution principal");
        scheduledAction.ClaimId.Should().BeNull();
        scheduledAction.LeaseExpiresAt.Should().BeNull();
        task.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScheduledActions_reclaims_action_after_worker_lease_expires()
    {
        await using var scope = await Fixture.CreateScope();
        var setup = await ScheduleDueTaskDeletion(scope);

        setup.ScheduledAction.Status = ScheduledAutomationActionStatus.Processing;
        setup.ScheduledAction.AttemptCount = 1;
        setup.ScheduledAction.ClaimId = Guid.NewGuid();
        setup.ScheduledAction.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);
        var deletedTask = await scope.Db.ProjectTasks
            .IgnoreQueryFilters()
            .SingleAsync(task => task.Id == setup.Scenario.Task.Id, TestContext.Current.CancellationToken);

        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Completed);
        scheduledAction.AttemptCount.Should().Be(2);
        scheduledAction.ClaimId.Should().BeNull();
        scheduledAction.LeaseExpiresAt.Should().BeNull();
        deletedTask.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteScheduledActions_cancels_when_current_conditions_no_longer_match()
    {
        await using var scope = await Fixture.CreateScope();
        var conditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Priority,
                    Operator = AutomationConditionOperator.Equals,
                    Value = TaskPriority.High.ToString(),
                },
            ],
        };
        var setup = await ScheduleDueTaskDeletion(scope, TaskPriority.High, conditionGroup);

        await scope.Db.ProjectTasks
            .Where(task => task.Id == setup.Scenario.Task.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(task => task.Priority, TaskPriority.Low),
                TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);
        var task = await scope.Db.ProjectTasks.SingleAsync(
            task => task.Id == setup.Scenario.Task.Id,
            TestContext.Current.CancellationToken);

        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Cancelled);
        scheduledAction.AttemptCount.Should().Be(1);
        scheduledAction.LastError.Should().Contain("no longer matches");
        task.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScheduledActions_fails_when_permission_is_revoked_after_scheduling()
    {
        await using var scope = await Fixture.CreateScope();
        var setup = await ScheduleDueTaskDeletion(scope);
        var membership = await scope.Db.WorkspaceAppUsers.SingleAsync(
            item => item.UserId == setup.Scenario.ExecutionUser.Id,
            TestContext.Current.CancellationToken);
        membership.Permissions = [];
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await scope.AutomationExecution.ExecuteScheduledActions(TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);
        var task = await scope.Db.ProjectTasks.SingleAsync(
            item => item.Id == setup.Scenario.Task.Id,
            TestContext.Current.CancellationToken);

        scheduledAction.Status.Should().Be(ScheduledAutomationActionStatus.Failed);
        scheduledAction.AttemptCount.Should().Be(1);
        scheduledAction.LastError.Should().Contain("permission required");
        task.IsDeleted.Should().BeFalse();
    }

    private static async Task<(AutomationScenario Scenario, ScheduledAutomationAction ScheduledAction)>
        ScheduleDueTaskDeletion(
            AutomationTestScope scope,
            TaskPriority? priority = null,
            AutomationConditionGroup? conditionGroup = null)
    {
        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        scenario.Task.Priority = priority;
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var statusCondition = new AutomationFieldCondition
        {
            Field = TaskChangeField.Status,
            Operator = AutomationConditionOperator.Equals,
            Value = completeStatusId.ToString(),
        };
        var conditions = conditionGroup?.Conditions.Prepend(statusCondition).ToList() ?? [statusCondition];
        var groups = conditionGroup?.Groups ?? [];
        var scheduledConditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions = conditions,
            Groups = groups,
        };
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            scheduledConditionGroup,
            actionType: AutomationActionType.DeleteTask);
        await ConfigureDeleteDelay(scope.Db, rule, 1, AutomationDelayUnit.Hours);

        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();

        var scheduledAction = await scope.Db.ScheduledAutomationActions.SingleAsync(
            TestContext.Current.CancellationToken);
        scheduledAction.ExecuteAt = DateTime.UtcNow.AddMinutes(-1);
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (scenario, scheduledAction);
    }

    private static AutomationConditionGroup CreateStatusConditionGroup(int statusId)
    {
        return new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Status,
                    Operator = AutomationConditionOperator.Equals,
                    Value = statusId.ToString(),
                },
            ],
        };
    }

    private static async Task ConfigureDeleteDelay(
        DataContext db,
        AutomationRule rule,
        int delayAmount,
        AutomationDelayUnit delayUnit)
    {
        var action = await db.AutomationActions.SingleAsync(
            item => item.AutomationRuleId == rule.Id && item.Type == AutomationActionType.DeleteTask,
            TestContext.Current.CancellationToken);

        action.Config = JsonSerializer.SerializeToDocument(new
        {
            delayAmount,
            delayUnit,
        }, JsonOptions.Default);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteUnassignedRules_creates_notification_for_eligible_unassigned_task()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(
            scope.Db,
            assignTask: false,
            taskUpdatedAt: DateTime.UtcNow.AddDays(-3));

        await AutomationTestData.CreateUnassignedRule(
            scope.Db,
            scenario,
            durationDays: 2,
            AutomationActionType.NotifyTaskAssignees);

        await scope.AutomationExecution.ExecuteUnassignedRules(TestContext.Current.CancellationToken);

        var notification = await scope.Db.Notifications.SingleAsync(TestContext.Current.CancellationToken);
        var activityLog = await scope.Db.EventRecords.SingleAsync(TestContext.Current.CancellationToken);
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        notification.UserId.Should().Be(scenario.Owner.Id);
        notification.EventRecordId.Should().Be(activityLog.Id);
        notification.Link.Should().Be($"/{scenario.Workspace.Slug}/tasks/{scenario.Project.Key}-{scenario.Task.ProjectScopeId}");
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_matches_scalar_field_condition()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        scenario.Task.Name = "Urgent request";

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            conditionGroup: new AutomationConditionGroup
            {
                Operator = AutomationConditionGroupOperator.All,
                Conditions =
                [
                    new AutomationFieldCondition
                    {
                        Field = TaskChangeField.Name,
                        Operator = AutomationConditionOperator.Contains,
                        Value = "urgent",
                    },
                ],
            });

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "Urgent request"),
            ],
        }, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_skips_nonmatching_scalar_field_condition()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Priority],
            conditionGroup: new AutomationConditionGroup
            {
                Operator = AutomationConditionGroupOperator.All,
                Conditions =
                [
                    new AutomationFieldCondition
                    {
                        Field = TaskChangeField.Priority,
                        Operator = AutomationConditionOperator.Equals,
                        Value = "Critical",
                    },
                ],
            });

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Priority, TaskPriority.Low, TaskPriority.High),
            ],
        }, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_matches_specific_collection_value()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Tags],
            conditionGroup: new AutomationConditionGroup
            {
                Operator = AutomationConditionGroupOperator.All,
                Conditions =
                [
                    new AutomationFieldCondition
                    {
                        Field = TaskChangeField.Tags,
                        Operator = AutomationConditionOperator.Added,
                        Value = "Release",
                    },
                ],
            });

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Tags(["release"], []),
            ],
        }, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_matches_all_current_state_conditions()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        scenario.Task.Priority = TaskPriority.High;

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var conditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Status,
                    Operator = AutomationConditionOperator.Equals,
                    Value = inProgressStatusId.ToString(),
                },
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Priority,
                    Operator = AutomationConditionOperator.Equals,
                    Value = TaskPriority.High.ToString(),
                },
            ],
        };

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            conditionGroup: conditionGroup);

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", scenario.Task.Name),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_requires_every_condition_in_all_group()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        var conditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Name,
                    Operator = AutomationConditionOperator.Contains,
                    Value = "Automation",
                },
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Priority,
                    Operator = AutomationConditionOperator.Equals,
                    Value = TaskPriority.Critical.ToString(),
                },
            ],
        };

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            conditionGroup: conditionGroup);

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", scenario.Task.Name),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_matches_nested_any_and_none_groups()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        var conditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Groups =
            [
                new AutomationConditionGroup
                {
                    Operator = AutomationConditionGroupOperator.Any,
                    Conditions =
                    [
                        new AutomationFieldCondition
                        {
                            Field = TaskChangeField.Priority,
                            Operator = AutomationConditionOperator.Equals,
                            Value = TaskPriority.High.ToString(),
                        },
                        new AutomationFieldCondition
                        {
                            Field = TaskChangeField.Name,
                            Operator = AutomationConditionOperator.Contains,
                            Value = "Automation",
                        },
                    ],
                },
                new AutomationConditionGroup
                {
                    Operator = AutomationConditionGroupOperator.None,
                    Conditions =
                    [
                        new AutomationFieldCondition
                        {
                            Field = TaskChangeField.Description,
                            Operator = AutomationConditionOperator.Contains,
                            Value = "cancelled",
                        },
                    ],
                },
            ],
        };

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            conditionGroup: conditionGroup);

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", scenario.Task.Name),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteDueDateRules_creates_run_for_task_due_on_configured_day()
    {
        await using var scope = await Fixture.CreateScope();

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        var scenario = await AutomationTestData.CreateScenario(scope.Db, dueDate: dueDate);
        var rule = await AutomationTestData.CreateDueDateRule(scope.Db, scenario, durationDays: 3);

        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        var flag = await scope.Db.Flags.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.TriggerType.Should().Be(AutomationTriggerType.TaskDueDateApproaching);
        run.IdempotencyKey.Should().EndWith($":due:{dueDate:yyyy-MM-dd}");
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
        flag.EntityId.Should().Be(scenario.Task.Id);
    }

    [Fact]
    public async Task ExecuteDueDateRules_is_idempotent_for_same_due_date()
    {
        await using var scope = await Fixture.CreateScope();

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var scenario = await AutomationTestData.CreateScenario(scope.Db, dueDate: dueDate);

        await AutomationTestData.CreateDueDateRule(scope.Db, scenario, durationDays: 0);

        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
        flagCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteDueDateRules_does_not_run_before_configured_day()
    {
        await using var scope = await Fixture.CreateScope();

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(4);
        var scenario = await AutomationTestData.CreateScenario(scope.Db, dueDate: dueDate);

        await AutomationTestData.CreateDueDateRule(scope.Db, scenario, durationDays: 3);

        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(0);
        flagCount.Should().Be(0);
    }

    private static DateOnly AddBusinessDays(DateOnly date, int offset)
    {
        var remaining = Math.Abs(offset);
        var direction = Math.Sign(offset);
        var result = date;

        while (remaining > 0)
        {
            result = result.AddDays(direction);
            var isWeekday = result.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

            if (isWeekday)
            {
                remaining--;
            }
        }

        return result;
    }
}
