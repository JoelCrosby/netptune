using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Users;
using Netptune.Services.Activity;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.ProjectTasks;

public class ProjectTaskDiffTests
{
    private const int EntityId = 42;
    private const int WorkspaceId = 7;

    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IActivityLogger Logger;

    private readonly List<ActivityMessage> Dispatched = [];

    public ProjectTaskDiffTests()
    {
        var identity = Substitute.For<IIdentityService>();

        identity.GetCurrentUserId().Returns("user-1");
        identity.GetWorkspaceId().Returns(WorkspaceId);

        EventPublisher
            .When(publisher => publisher.Dispatch(Arg.Any<ActivityMessage>()))
            .Do(callInfo => Dispatched.Add(callInfo.Arg<ActivityMessage>()));

        Logger = new ActivityLogger(EventPublisher, identity, new HttpContextAccessor());
    }

    private static TaskViewModel Task(
        string name = "task",
        string? description = "description",
        int statusId = 1,
        TaskPriority? priority = TaskPriority.Low,
        EstimateType? estimateType = EstimateType.StoryPoints,
        decimal? estimateValue = 3,
        DateOnly? startDate = null,
        DateOnly? dueDate = null,
        params string[] assignees)
    {
        return new TaskViewModel
        {
            Id = EntityId,
            Name = name,
            Description = description,
            StatusId = statusId,
            Priority = priority,
            EstimateType = estimateType,
            EstimateValue = estimateValue,
            StartDate = startDate,
            DueDate = dueDate,
            Assignees = assignees.Select(id => new AssigneeViewModel { Id = id }).ToList(),
        };
    }

    [Fact]
    public void LogDiff_ShouldLogDueDateChangesIncludingClearingTheDate()
    {
        var old = Task(dueDate: new DateOnly(2026, 7, 14));
        var updated = Task(dueDate: null);

        var dueDate = LogDiff(old, updated).Events.Single();

        dueDate.Type.Should().Be(ActivityType.ModifyDueDate);
        dueDate.Field.Should().Be(TaskChangeField.DueDate);
        dueDate.OldValue.Should().Be("2026-07-14");
        dueDate.NewValue.Should().BeNull();
    }

    [Fact]
    public void LogDiff_ShouldLogStartDateChangesIncludingClearingTheDate()
    {
        var old = Task(startDate: new DateOnly(2026, 7, 10));
        var updated = Task(startDate: null);

        var startDate = LogDiff(old, updated).Events.Single();

        startDate.Type.Should().Be(ActivityType.ModifyStartDate);
        startDate.Field.Should().Be(TaskChangeField.StartDate);
        startDate.OldValue.Should().Be("2026-07-10");
        startDate.NewValue.Should().BeNull();
    }

    private ActivityMessage LogDiff(TaskViewModel old, TaskViewModel updated)
    {
        ProjectTaskDiff.Create(old, updated).LogDiff(Logger, EntityId);

        Dispatched.Should().HaveCount(1, "a single task update must dispatch exactly one ActivityMessage");

        return Dispatched[0];
    }

    [Fact]
    public void LogDiff_ShouldDispatchOneMessageCarryingEveryEvent_WhenManyFieldsChange()
    {
        var old = Task("old name", "old description");
        var updated = Task("new name", "new description", 2, TaskPriority.High, EstimateType.Hours, 5);

        var message = LogDiff(old, updated);

        message.Events.Should().HaveCount(5);
        message.Events.Select(activity => activity.Type).Should().BeEquivalentTo(
        [
            ActivityType.ModifyName,
            ActivityType.ModifyDescription,
            ActivityType.ModifyStatus,
            ActivityType.ModifyPriority,
            ActivityType.ModifyEstimate,
        ]);

        message.Events.Should().OnlyContain(activity =>
            activity.EntityId == EntityId
            && activity.EntityType == EntityType.Task
            && activity.WorkspaceId == WorkspaceId
            && activity.UserId == "user-1");
    }

    [Fact]
    public void LogDiff_ShouldCarryOldAndNewValues_ForEveryField()
    {
        var old = Task("old name", "old description");
        var updated = Task("new name", "new description", 2, TaskPriority.High, EstimateType.Hours, 5);

        var events = LogDiff(old, updated).Events.ToDictionary(activity => activity.Field!.Value);

        events[TaskChangeField.Name].Should().Match<ActivityEvent>(e => e.OldValue == "old name" && e.NewValue == "new name");
        events[TaskChangeField.Description].Should().Match<ActivityEvent>(e => e.OldValue == "old description" && e.NewValue == "new description");
        events[TaskChangeField.Status].Should().Match<ActivityEvent>(e => e.OldValue == "1" && e.NewValue == "2");
        events[TaskChangeField.Priority].Should().Match<ActivityEvent>(e => e.OldValue == "Low" && e.NewValue == "High");
        events[TaskChangeField.Estimate].Should().Match<ActivityEvent>(e => e.OldValue == "StoryPoints" && e.NewValue == "Hours");
    }

    [Fact]
    public void LogDiff_ShouldTruncateLongValues()
    {
        var oldDescription = new string('a', 5000);
        var newDescription = new string('b', 5000);

        var message = LogDiff(
            Task(description: oldDescription),
            Task(description: newDescription));

        var description = message.Events.Single(activity => activity.Field == TaskChangeField.Description);

        description.OldValue.Should().Be(new string('a', ActivityValue.MaxLength));
        description.NewValue.Should().Be(new string('b', ActivityValue.MaxLength));
        ActivityValue.MaxLength.Should().Be(256);
    }

    [Fact]
    public void LogDiff_ShouldGiveEveryEventItsOwnEventId()
    {
        var old = Task("old name", "old description");
        var updated = Task("new name", "new description", 2, TaskPriority.High, EstimateType.Hours, 5);

        var eventIds = LogDiff(old, updated).Events.Select(activity => activity.EventId).ToList();

        eventIds.Should().OnlyHaveUniqueItems();
        eventIds.Should().NotContain(Guid.Empty);
    }

    [Fact]
    public void LogDiff_ShouldIncludeAssigneeEvents_InTheSameMessage()
    {
        var old = Task(assignees: ["user-2"]);
        var updated = Task(assignees: ["user-3"]);

        var message = LogDiff(old, updated);

        message.Events.Should().HaveCount(2);
        message.Events.Select(activity => activity.Type).Should().BeEquivalentTo(
        [
            ActivityType.Assign,
            ActivityType.Unassign,
        ]);

        message.Events.Single(activity => activity.Type == ActivityType.Assign)
            .Meta.Should().Contain("user-3");

        message.Events.Single(activity => activity.Type == ActivityType.Unassign)
            .Meta.Should().Contain("user-2");
    }

    [Fact]
    public void ToTaskFieldChanges_ShouldIncludeAddedAndRemovedTags()
    {
        var old = Task();
        old.Tags = ["Architecture", "Bug"];
        var updated = Task();
        updated.Tags = ["Architecture", "Feature"];

        var change = ProjectTaskDiff.Create(old, updated)
            .ToTaskFieldChanges()
            .Single(field => field.Field == TaskChangeField.Tags);

        change.AddedValues.Should().Equal("Feature");
        change.RemovedValues.Should().Equal("Bug");
    }

    [Fact]
    public void LogDiff_ShouldDispatchNothing_WhenNothingChanged()
    {
        var task = Task();

        ProjectTaskDiff.Create(task, task).LogDiff(Logger, EntityId);

        Dispatched.Should().BeEmpty();
    }
}
