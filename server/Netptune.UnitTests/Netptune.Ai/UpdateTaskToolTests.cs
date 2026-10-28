using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Core.ViewModels.Tags;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tasks.Queries;
using Netptune.Handlers.Users.Queries;

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
    public async Task Execute_ShouldLeaveBlankTextFieldsOutOfThePayload_SinceTheReviewDoesNotShowThem()
    {
        GivenTask(CreateTask() with { Description = "Keep me" });

        var result = await Execute($$"""{"taskId":{{TaskId}},"priority":"High","name":"  ","description":""}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Single();
        var payload = change.Payload.RootElement;

        change.Fields.Select(field => field.Name).Should().Equal("priority");
        payload.TryGetProperty("name", out _).Should().BeFalse();
        payload.TryGetProperty("description", out _).Should().BeFalse();
        payload.GetProperty("priority").GetString().Should().Be("High");
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
    public async Task Execute_ShouldFail_WhenTheStartDateWouldFallAfterTheStoredDueDate()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"startDate":"2026-09-01"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("2026-08-14");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldAllowALaterStartDate_WhenTheDueDateIsClearedAlongsideIt()
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"startDate":"2026-09-01","clear":["dueDate"]}""");

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheBoardGroupIsOnAnotherProjectsBoard()
    {
        GivenTask(CreateTask());
        GivenBoardGroup(new BoardGroupOptionViewModel
        {
            Id = 9,
            Name = "Todo",
            BoardId = 4,
            BoardName = "Marketing",
            BoardIdentifier = "marketing",
            ProjectId = 7,
            ProjectName = "Marketing",
        });

        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupId":9}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("its own project");
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

    [Fact]
    public async Task Execute_ShouldProposeAMove_IntoABoardGroupPendingInTheSameChangeSet()
    {
        GivenTask(CreateTask());

        var groupRef = ProposePendingBoardGroup("""{"projectId":3}""");
        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupRef":"{{groupRef}}"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Last();

        change.ToolName.Should().Be("propose_move_task_to_board_group");
        change.Payload.RootElement.GetProperty("boardGroupRef").GetString().Should().Be(groupRef);
        change.Fields.Single().After.Should().Be("Slash Commands · Todo");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenThePendingBoardGroupIsInAnotherProject()
    {
        GivenTask(CreateTask());

        var groupRef = ProposePendingBoardGroup("""{"projectId":7}""");
        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupRef":"{{groupRef}}"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("different project");
    }

    [Fact]
    public void GetRequiredPermissions_ShouldDemandMove_ForABoardGroupRef()
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse("""{"taskId":42,"boardGroupRef":"ref:2"}""").RootElement;

        tool.GetRequiredPermissions(arguments).Should().Contain(NetptunePermissions.Tasks.Move);
    }

    [Fact]
    public void GetRequiredPermissions_ShouldDemandMove_ForABoardRemoval()
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse("""{"taskId":42,"removeFromBoardIds":[5]}""").RootElement;

        tool.GetRequiredPermissions(arguments).Should().Contain(NetptunePermissions.Tasks.Move);
    }

    [Fact]
    public async Task Execute_ShouldMoveATaskToAnotherBoard_WhenItIsTakenOffTheBoardItWasOn()
    {
        GivenTask(CreateTask() with { BoardGroupId = 11, Placements = [CreatePlacement(5, "Legacy", 11, "Todo")] });
        GivenBoardGroup(CreateGroup(9, projectId: 3));

        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupId":9,"removeFromBoardIds":[5]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Select(change => change.ToolName)
            .Should().Equal("propose_move_task_to_board_group", "propose_remove_task_from_board");

        var removal = ChangeSet.Changes[1];

        removal.Payload.RootElement.GetProperty("boardId").GetInt32().Should().Be(5);
        removal.Fields.Single().BeforeValues!.Single().Display.Should().Be("Legacy · Todo");
        removal.Fields.Single().AfterValues.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldTakeATaskOffABoard_WithoutMovingIt()
    {
        GivenTask(CreateTask() with { Placements = [CreatePlacement(5, "Legacy", 11, "Todo")] });

        var result = await Execute($$"""{"taskId":{{TaskId}},"removeFromBoardIds":[5]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Single().ToolName.Should().Be("propose_remove_task_from_board");
    }

    [Fact]
    public async Task Execute_ShouldRefuseABoardRemoval_ForABoardTheTaskIsNotOn()
    {
        GivenTask(CreateTask() with { Placements = [CreatePlacement(5, "Legacy", 11, "Todo")] });

        var result = await Execute($$"""{"taskId":{{TaskId}},"removeFromBoardIds":[8]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("is not on these boards: 8");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldRefuseTakingATaskOffTheBoardItIsMovingOnto()
    {
        GivenTask(CreateTask() with { BoardGroupId = 10, Placements = [CreatePlacement(4, "Netptune", 10, "Todo")] });
        GivenBoardGroup(CreateGroup(9, projectId: 3));

        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupId":9,"removeFromBoardIds":[4]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Netptune");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldProposeNoMove_WhenTheTaskIsAlreadyInTheGroupOnAnotherOfItsBoards()
    {
        GivenTask(CreateTask() with
        {
            BoardGroupId = 11,
            Placements = [CreatePlacement(5, "Legacy", 11, "Todo"), CreatePlacement(4, "Netptune", 9, "Shipped")],
        });
        GivenBoardGroup(CreateGroup(9, projectId: 3));

        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupId":9}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    private static BoardPlacementViewModel CreatePlacement(int boardId, string boardName, int groupId, string groupName)
    {
        return new BoardPlacementViewModel
        {
            BoardId = boardId,
            BoardName = boardName,
            BoardIdentifier = boardName.ToLowerInvariant(),
            BoardGroupId = groupId,
            BoardGroupName = groupName,
        };
    }

    private string ProposePendingBoardGroup(string boardPayload)
    {
        var boardRef = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_board",
            EntityType = "board",
            RefKey = boardRef,
            Summary = "Create board “Slash Commands”",
            Fields = [new AiChangeField { Name = "name", After = "Slash Commands" }],
            Payload = JsonDocument.Parse(boardPayload),
        });

        var groupRef = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_board_group",
            EntityType = "boardGroup",
            RefKey = groupRef,
            Summary = "Add group “Todo” to Slash Commands",
            Fields =
            [
                new AiChangeField { Name = "name", After = "Todo" },
                new AiChangeField { Name = "board", After = "Slash Commands" },
            ],
            Payload = JsonDocument.Parse($$"""{"name":"Todo","boardRef":"{{boardRef}}"}"""),
        });

        return groupRef;
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

        AiTestTags.Given(Mediator, tags);
    }

    [Fact]
    public async Task Execute_ShouldProposeEveryUpdateInABatch()
    {
        GivenTasks(CreateTask(), CreateTask() with { Id = 43, SystemId = "NPT-43", Name = "Tidy the docs" });

        var result = await Execute(
            """{"updates":[{"taskId":42,"description":"One line."},{"taskId":43,"priority":"High"}]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Select(change => change.EntityId).Should().Equal(42, 43);
        await Mediator.Received(1).Send(
            Arg.Is<GetTasksByIdQuery>(query => query.Ids.SequenceEqual(new[] { 42, 43 })),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<GetTaskQuery>(), Arg.Any<CancellationToken>());

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("proposed").EnumerateArray().Select(task => task.GetString()).Should().Equal("42", "43");
        content.RootElement.GetProperty("refused").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Execute_ShouldKeepTheUpdatesThatWork_AndSayWhyTheOthersWereRefused()
    {
        GivenTasks(CreateTask());

        var result = await Execute("""{"updates":[{"taskId":42,"priority":"High"},{"taskId":99,"priority":"High"}]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle();

        using var content = JsonDocument.Parse(result.Content);
        var refused = content.RootElement.GetProperty("refused").EnumerateArray().Single();

        refused.GetProperty("task").GetString().Should().Be("99");
        refused.GetProperty("reason").GetString().Should().Contain("not found");
    }

    [Theory]
    [InlineData("""{"updates":[]}""", "empty")]
    [InlineData("""{"updates":[{"taskId":42,"priority":"High"},{"taskId":42,"name":"x"}]}""", "only once")]
    [InlineData("""{"taskId":42,"updates":[{"taskId":42,"priority":"High"}]}""", "not both")]
    [InlineData("""{"updates":[{"taskId":99,"priority":"High"}]}""", "not found")]
    [InlineData("""{"updates":[{"priority":"High"},{"name":"x"}]}""", "taskId is required")]
    public async Task Execute_ShouldRefuseABatch_ItCannotPropose(string arguments, string message)
    {
        GivenTasks(CreateTask());

        var result = await Execute(arguments);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain(message);
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public void GetRequiredPermissions_ShouldDemandWhatEveryUpdateInABatchChanges()
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse("""{"updates":[{"taskId":1,"tags":["Ops"]},{"taskId":2,"priority":"High"}]}""").RootElement;

        tool.GetRequiredPermissions(arguments)
            .Should().BeEquivalentTo([NetptunePermissions.Tags.Assign, NetptunePermissions.Tasks.Update]);
    }

    private async Task<AiToolExecution> Execute(string arguments)
    {
        var tool = new UpdateTaskTool(Mediator, ChangeSet);
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Execute_ShouldShowTheStatusABoardGroupWillGiveTheTask()
    {
        GivenTask(CreateTask());
        GivenBoardGroup(CreateGroup(9, projectId: 3));
        GivenGroupStatus(9, new StatusViewModel { Id = 4, Name = "Done", Key = "done" });

        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupId":9}""");

        result.IsError.Should().BeFalse();

        var fields = ChangeSet.Changes.Single().Fields;

        fields.Select(field => field.Name).Should().Equal("boardGroup", "status");
        fields[1].AfterValues!.Single().Display.Should().Be("Done");
    }

    [Fact]
    public async Task Execute_ShouldRefuseAStatus_ThatTheBoardGroupWouldOverwrite()
    {
        GivenTask(CreateTask());
        GivenBoardGroup(CreateGroup(9, projectId: 3));
        GivenGroupStatus(9, new StatusViewModel { Id = 4, Name = "Done", Key = "done" });

        var result = await Execute($$"""{"taskId":{{TaskId}},"boardGroupId":9,"statusId":2}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Done");
    }

    [Fact]
    public async Task Execute_ShouldAssignAMember_LookedUpById()
    {
        GivenTask(CreateTask());

        Mediator
            .Send(Arg.Any<GetAssigneesByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns([new AssigneeViewModel { Id = "member-150", DisplayName = "Member 150" }]);

        var result = await Execute($$"""{"taskId":{{TaskId}},"assigneeIds":["member-150"]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Single().Fields.Single().AfterValues!.Single().Display.Should().Be("Member 150");
    }

    [Fact]
    public async Task Execute_ShouldRefuseAnAssigneeWhoIsNotAMember()
    {
        GivenTask(CreateTask());

        Mediator
            .Send(Arg.Any<GetAssigneesByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<AssigneeViewModel>());

        var result = await Execute($$"""{"taskId":{{TaskId}},"assigneeIds":["stranger"]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("stranger");
    }

    [Theory]
    [InlineData("99")]
    [InlineData("1")]
    [InlineData("High, Low")]
    public async Task Execute_ShouldRefuseAPriority_ThatIsNotANamedLevel(string priority)
    {
        GivenTask(CreateTask());

        var result = await Execute($$"""{"taskId":{{TaskId}},"priority":"{{priority}}"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldReadMembersAndBoardGroupsOnce_ForAWholeBatch()
    {
        var tasks = Enumerable.Range(1, 3)
            .Select(id => CreateTask() with { Id = id, SystemId = $"NPT-{id}" })
            .ToArray();

        GivenTasks(tasks);
        GivenBoardGroup(CreateGroup(9, projectId: 3));
        Mediator
            .Send(Arg.Any<GetAssigneesByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns([new AssigneeViewModel { Id = "alice", DisplayName = "Alice" }]);

        var updateJson = tasks.Select(task => $$"""{"taskId":{{task.Id}},"assigneeIds":["alice"],"boardGroupId":9}""");
        var updates = string.Join(",", updateJson);
        var result = await Execute($$"""{"updates":[{{updates}}]}""");

        result.IsError.Should().BeFalse(result.Content);
        await Mediator.Received(1).Send(Arg.Any<GetAssigneesByIdQuery>(), Arg.Any<CancellationToken>());
        await Mediator.Received(1).Send(Arg.Any<GetBoardGroupOptionsQuery>(), Arg.Any<CancellationToken>());
    }

    private void GivenGroupStatus(int groupId, StatusViewModel status)
    {
        Mediator
            .Send(Arg.Is<GetBoardGroupQuery>(query => query.Id == groupId), Arg.Any<CancellationToken>())
            .Returns(new BoardGroup { Id = groupId, Name = "Group", StatusId = status.Id });
        Mediator
            .Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns([status, new StatusViewModel { Id = 2, Name = "Doing", Key = "doing" }]);
    }

    private static BoardGroupOptionViewModel CreateGroup(int id, int projectId)
    {
        return new BoardGroupOptionViewModel
        {
            Id = id,
            Name = "Shipped",
            BoardId = 4,
            BoardName = "Netptune",
            BoardIdentifier = "netptune",
            ProjectId = projectId,
            ProjectName = "Netptune",
        };
    }

    private void GivenTask(TaskViewModel task)
    {
        Mediator
            .Send(Arg.Is<GetTaskQuery>(query => query.Id == task.Id), Arg.Any<CancellationToken>())
            .Returns(task);
    }

    private void GivenBoardGroup(BoardGroupOptionViewModel group)
    {
        Mediator
            .Send(Arg.Any<GetBoardGroupOptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns([group]);
    }

    private void GivenTasks(params TaskViewModel[] tasks)
    {
        Mediator
            .Send(Arg.Any<GetTasksByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks.ToList());
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
