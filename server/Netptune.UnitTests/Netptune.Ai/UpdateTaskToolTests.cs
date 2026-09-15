using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class UpdateTaskToolTests
{
    private const int TaskId = 42;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    [Fact]
    public async Task Execute_ShouldProposeAPriorityChange()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"priority":"High"}""");

        result.IsError.Should().BeFalse();

        var field = ChangeSet.Changes.Single().Fields.Single();

        field.Name.Should().Be("priority");
        field.Before.Should().Be(nameof(TaskPriority.Low));
        field.After.Should().Be(nameof(TaskPriority.High));
    }

    [Fact]
    public async Task Execute_ShouldCarryADateChangeAsATypedValue()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"dueDate":"2026-09-01"}""");

        result.IsError.Should().BeFalse();

        var field = ChangeSet.Changes.Single().Fields.Single();

        field.Name.Should().Be("dueDate");
        field.Kind.Should().Be(AiChangeValueKind.Date);
        field.BeforeValues!.Single().Display.Should().Be("2026-08-14");
        field.AfterValues!.Single().Display.Should().Be("2026-09-01");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenThePriorityIsNotAKnownLevel()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"priority":"Urgent"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Critical");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldProposeClearingADate()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"clear":["dueDate"]}""");

        result.IsError.Should().BeFalse();

        var field = ChangeSet.Changes.Single().Fields.Single();

        field.Name.Should().Be("dueDate");
        field.Before.Should().Be("2026-08-14");
        field.After.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenADateIsBothSetAndCleared()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"dueDate":"2026-09-01","clear":["dueDate"]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenADateIsNotReadable()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"startDate":"next friday"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("YYYY-MM-DD");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldProposeAnEstimateChange()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"estimateValue":8}""");

        result.IsError.Should().BeFalse();

        var field = ChangeSet.Changes.Single().Fields.Single();

        field.Name.Should().Be("estimate");
        field.Before.Should().Be("3 StoryPoints");
        field.After.Should().Be("8 StoryPoints");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenAnEstimateHasNoUnitToMeasureItIn()
    {
        GivenTask(CreateTask() with { EstimateType = null, EstimateValue = null });

        var result = await Execute($$"""{"taskId":{{TaskId}},"estimateValue":8}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("estimateType");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenATShirtEstimateIsOutOfRange()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"estimateType":"TShirt","estimateValue":9}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldDiffTheStoredDescriptionAgainstTheProposedMarkdown()
    {
        GivenTask(CreateTask() with { Description = "## Steps" });

        var result = await Execute($$"""{"taskId":{{TaskId}},"description":"## Steps\n\nAnd a line."}""");

        result.IsError.Should().BeFalse();

        var field = ChangeSet.Changes.Single().Fields.Single();

        field.Name.Should().Be("description");
        field.Before.Should().Be("## Steps");
        field.After.Should().Be("## Steps\n\nAnd a line.");
    }

    [Fact]
    public async Task Execute_ShouldProposeFieldsAndTagsAsSeparateChanges()
    {
        GivenTask(CreateTask());
        GivenTags("bug");

        var result = await Execute($$"""{"taskId":{{TaskId}},"priority":"High","tags":["bug"]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Select(change => change.ToolName).Should().Equal("propose_update_task", "propose_set_task_tags");

        var fieldPayload = ChangeSet.Changes[0].Payload.RootElement;

        fieldPayload.TryGetProperty("tags", out _).Should().BeFalse();
        fieldPayload.GetProperty("priority").GetString().Should().Be("High");
    }

    [Fact]
    public async Task Execute_ShouldProposeNothing_WhenAnyPartFails()
    {
        GivenTask(CreateTask());
        GivenTags("bug");

        var result = await Execute($$"""{"taskId":{{TaskId}},"priority":"High","tags":["unknown"]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheTagsAreAlreadyOnTheTask()
    {
        GivenTask(CreateTask() with { Tags = ["bug"] });
        GivenTags("bug");

        var result = await Execute($$"""{"taskId":{{TaskId}},"tags":["Bug"]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldTagATaskPendingInTheSameChangeSet()
    {
        GivenTags("bug");

        var taskRef = ProposePendingTask();
        var result = await Execute($$"""{"taskRef":"{{taskRef}}","tags":["bug"]}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Last();

        change.ToolName.Should().Be("propose_set_task_tags");
        change.Payload.RootElement.GetProperty("taskRef").GetString().Should().Be(taskRef);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenATaskPendingInTheSameChangeSetIsGivenMoreThanTags()
    {
        var taskRef = ProposePendingTask();
        var result = await Execute($$"""{"taskRef":"{{taskRef}}","priority":"High"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().ContainSingle();
    }

    [Fact]
    public void GetRequiredPermissions_ShouldDemandOnlyWhatTheArgumentsChange()
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var tagsOnly = JsonDocument.Parse("""{"taskId":1,"tags":["bug"]}""").RootElement;
        var priorityAndAssignees = JsonDocument.Parse("""{"taskId":1,"priority":"High","assigneeIds":[]}""").RootElement;

        tool.GetRequiredPermissions(tagsOnly).Should().BeEquivalentTo([NetptunePermissions.Tags.Assign]);
        tool.GetRequiredPermissions(priorityAndAssignees)
            .Should()
            .BeEquivalentTo([NetptunePermissions.Tasks.Update, NetptunePermissions.Tasks.Reassign]);
    }

    [Fact]
    public void IsAvailable_ShouldOfferTheTool_ToAMemberWhoCanOnlyTag()
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var tagOnly = new HashSet<string> { NetptunePermissions.Tasks.Read, NetptunePermissions.Tags.Assign };
        var readOnly = new HashSet<string> { NetptunePermissions.Tasks.Read };

        tool.IsAvailable(tagOnly).Should().BeTrue();
        tool.IsAvailable(readOnly).Should().BeFalse();
    }

    private string ProposePendingTask()
    {
        var refKey = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_task",
            EntityType = "task",
            RefKey = refKey,
            Summary = "Create task “Wire up the loader”",
            Fields = [new AiChangeField { Name = "name", After = "Wire up the loader" }],
            Payload = JsonDocument.Parse("""{"name":"Wire up the loader","projectId":3}"""),
        });

        return refKey;
    }

    private void GivenTags(params string[] names)
    {
        var tags = names.Select(name => new TagViewModel { Name = name }).ToList();

        Mediator
            .Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(tags);
    }

    private async Task<AiToolExecution> Execute(string arguments)
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }

    private void GivenTask(TaskViewModel task)
    {
        Mediator
            .Send(Arg.Is<GetTaskQuery>(query => query.Id == task.Id), Arg.Any<CancellationToken>())
            .Returns(task);
    }

    private static TaskViewModel CreateTask()
    {
        return new TaskViewModel
        {
            Id = TaskId,
            Name = "Fix the login page",
            SystemId = "NPT-42",
            ProjectId = 3,
            StatusId = 1,
            StatusName = "✨ New",
            Priority = TaskPriority.Low,
            EstimateType = EstimateType.StoryPoints,
            EstimateValue = 3,
            DueDate = new DateOnly(2026, 8, 14),
        };
    }
}
