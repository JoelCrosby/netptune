using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Sprints.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;
using Netptune.Handlers.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiChangeEditCatalogTests
{
    private const string CreateTask = "propose_create_task";
    private const string UpdateTask = "propose_update_task";
    private const string AssignTask = "propose_assign_task";
    private const string SetTags = "propose_set_task_tags";
    private const string MoveToBoardGroup = "propose_move_task_to_board_group";
    private const string MoveToSprint = "propose_move_task_to_sprint";
    private const string CreateTag = "propose_create_tag";
    private const int ProjectId = 3;
    private const int TaskId = 12;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeEditCatalog Catalog;

    public AiChangeEditCatalogTests()
    {
        Catalog = new AiChangeEditCatalog(Mediator);
    }

    [Fact]
    public async Task Apply_ShouldRewriteTheProposedValueInBothTheFieldsAndThePayload()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" });

        result.IsSuccess.Should().BeTrue();
        After(result, "name").Should().Be("Reviewer name");
        PayloadString(result, "name").Should().Be("Reviewer name");
    }

    [Fact]
    public async Task Apply_ShouldLeaveTheValuesItWasNotAskedToChangeAlone()
    {
        var change = Change(
            UpdateTask,
            "Update name, description on “Old name”",
            [
                AiChangeFields.Text("name", "Old name", "Assistant name"),
                AiChangeFields.Text("description", null, "Assistant description"),
            ],
            """{"taskId":12,"name":"Assistant name","description":"Assistant description"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "description", Value = "Reviewer description" });

        After(result, "name").Should().Be("Assistant name");
        After(result, "description").Should().Be("Reviewer description");
        PayloadString(result, "name").Should().Be("Assistant name");
    }

    [Fact]
    public async Task Apply_ShouldKeepTheBeforeSideOfTheDiffPointingAtWhatIsThereToday()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" });

        Fields(result).Single().Before.Should().Be("Old name");
    }

    [Fact]
    public async Task Apply_ShouldRequoteASummaryThatNamesTheValueTheChangeCreates()
    {
        var change = Change(
            CreateTask,
            "Create task “Assistant name”",
            [AiChangeFields.Text("name", null, "Assistant name")],
            """{"name":"Assistant name"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" });

        result.Summary.Should().Be("Create task “Reviewer name”");
    }

    [Fact]
    public async Task Apply_ShouldLeaveASummaryThatNamesTheEntityBeingChanged()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" });

        result.Summary.Should().Be("Update name on “Old name”");
    }

    [Fact]
    public async Task Apply_ShouldRefuseAFieldTheChangeDoesNotPropose()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "priority", Value = "High" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("priority");
    }

    [Fact]
    public async Task Apply_ShouldRefuseAnEmptyValue()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "name", Value = "   " });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task Apply_ShouldRefuseAnEditWithNoFields()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name"}""");

        var result = await Catalog.Apply(change, [change], [], TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Apply_ShouldRefuseAValueLongerThanTheColumnHolds()
    {
        var change = Change(
            UpdateTask,
            "Update description on “Old name”",
            [AiChangeFields.Text("description", null, "Assistant description")],
            """{"taskId":12,"description":"Assistant description"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "description", Value = new string('a', 8001) });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("longer than");
    }

    [Fact]
    public async Task Apply_ShouldLeaveTheRestOfThePayloadIntact()
    {
        var change = Change(
            UpdateTask,
            "Update name on “Old name”",
            [AiChangeFields.Text("name", "Old name", "Assistant name")],
            """{"taskId":12,"name":"Assistant name","clear":["dueDate"]}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "name", Value = "Reviewer name" });

        var payload = result.Payload.RootElement;

        payload.GetProperty("taskId").GetInt32().Should().Be(TaskId);
        payload.GetProperty("clear").EnumerateArray().Single().GetString().Should().Be("dueDate");
    }

    [Fact]
    public void FindEditor_ShouldOfferNoTextEditor_WhenThePayloadStoresTheValueUnderAnotherKey()
    {
        var change = Change(
            "propose_create_page",
            "Create page “Plan”",
            [AiChangeFields.Text("name", null, "Plan")],
            """{"title":"Plan","kind":"Document"}""");

        Catalog.FindEditor(change, "name").Should().BeNull();
    }

    [Fact]
    public void FindEditor_ShouldOfferAMarkdownEditorForADescription()
    {
        var change = Change(
            UpdateTask,
            "Update description on “Task”",
            [AiChangeFields.Text("description", "Old", "New")],
            """{"taskId":12,"description":"New"}""");

        Catalog.FindEditor(change, "description")!.Kind.Should().Be(AiChangeEditorKind.Markdown);
    }

    [Fact]
    public void FindEditor_ShouldOfferNoSprintEditor_WhenTheTaskIsInAProjectProposedInTheSameSet()
    {
        var change = Change(
            CreateTask,
            "Create task “Task”",
            [SprintField([], [AiChangeFields.Sprint(null, "Sprint 1")])],
            """{"name":"Task","projectRef":"project-1","sprintRef":"sprint-1"}""");

        Catalog.FindEditor(change, "sprint").Should().BeNull();
    }

    [Fact]
    public void FindEditor_ShouldScopeASprintEditorToTheCreatedTasksProject()
    {
        var change = Change(
            CreateTask,
            "Create task “Task”",
            [SprintField([], [AiChangeFields.Sprint(4, "Sprint 4")])],
            """{"name":"Task","projectId":3,"sprintId":4}""");

        var editor = Catalog.FindEditor(change, "sprint")!;

        editor.Kind.Should().Be(AiChangeEditorKind.Sprint);
        editor.IsClearable.Should().BeTrue();
        editor.ProjectId.Should().Be(ProjectId);
    }

    [Fact]
    public async Task Apply_ShouldPointAStatusChangeAtThePickedStatus()
    {
        GivenStatuses(new StatusViewModel { Id = 7, Name = "Review", Color = "#123456" });

        var change = Change(
            UpdateTask,
            "Update status on “Task”",
            [StatusField(1, "Todo", 2, "Doing")],
            """{"taskId":12,"statusId":2}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "status", Value = "7" });

        result.IsSuccess.Should().BeTrue();
        result.Payload.RootElement.GetProperty("statusId").GetInt32().Should().Be(7);

        var field = Fields(result).Single();

        field.BeforeValues!.Single().Display.Should().Be("Todo");
        field.AfterValues!.Single().Display.Should().Be("Review");
        field.AfterValues!.Single().Color.Should().Be("#123456");
    }

    [Fact]
    public async Task Apply_ShouldRefuseAStatusOutsideTheWorkspace()
    {
        GivenStatuses(new StatusViewModel { Id = 7, Name = "Review" });

        var change = Change(
            UpdateTask,
            "Update status on “Task”",
            [StatusField(1, "Todo", 2, "Doing")],
            """{"taskId":12,"statusId":2}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "status", Value = "99" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not in this workspace");
    }

    [Fact]
    public async Task Apply_ShouldRefuseClearingAStatusAnUpdateMustSet()
    {
        var change = Change(
            UpdateTask,
            "Update status on “Task”",
            [StatusField(1, "Todo", 2, "Doing")],
            """{"taskId":12,"statusId":2}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "status", Value = null });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task Apply_ShouldSetADateAndTakeItOffTheClearList()
    {
        var change = Change(
            UpdateTask,
            "Update dueDate on “Task”",
            [AiChangeFields.Date("dueDate", new DateOnly(2026, 8, 14), null)],
            """{"taskId":12,"clear":["dueDate"]}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "dueDate", Value = "2026-09-01" });

        result.IsSuccess.Should().BeTrue();
        PayloadString(result, "dueDate").Should().Be("2026-09-01");
        result.Payload.RootElement.GetProperty("clear").EnumerateArray().Should().BeEmpty();
        Fields(result).Single().AfterValues!.Single().Display.Should().Be("2026-09-01");
    }

    [Fact]
    public async Task Apply_ShouldClearAnUpdatedDateThroughTheClearList()
    {
        var change = Change(
            UpdateTask,
            "Update dueDate on “Task”",
            [AiChangeFields.Date("dueDate", new DateOnly(2026, 8, 14), new DateOnly(2026, 9, 1))],
            """{"taskId":12,"dueDate":"2026-09-01"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "dueDate", Value = null });

        result.IsSuccess.Should().BeTrue();
        result.Payload.RootElement.TryGetProperty("dueDate", out _).Should().BeFalse();
        result.Payload.RootElement.GetProperty("clear").EnumerateArray().Single().GetString().Should().Be("dueDate");
        Fields(result).Single().AfterValues.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_ShouldLeaveACreatedTasksClearedDateOutOfThePayload()
    {
        var change = Change(
            CreateTask,
            "Create task “Task”",
            [AiChangeFields.Date("startDate", null, new DateOnly(2026, 9, 1))],
            """{"name":"Task","projectId":3,"startDate":"2026-09-01"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "startDate", Value = null });

        result.IsSuccess.Should().BeTrue();
        result.Payload.RootElement.TryGetProperty("startDate", out _).Should().BeFalse();
        result.Payload.RootElement.TryGetProperty("clear", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Apply_ShouldRefuseADateInAnotherFormat()
    {
        var change = Change(
            UpdateTask,
            "Update dueDate on “Task”",
            [AiChangeFields.Date("dueDate", null, new DateOnly(2026, 9, 1))],
            """{"taskId":12,"dueDate":"2026-09-01"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "dueDate", Value = "01/09/2026" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("YYYY-MM-DD");
    }

    [Fact]
    public async Task Apply_ShouldWriteAnEstimateAsItsUnitAndValue()
    {
        var change = Change(
            UpdateTask,
            "Update estimate on “Task”",
            [AiChangeFields.Text("estimate", "3 StoryPoints", "5 StoryPoints")],
            """{"taskId":12,"estimateType":"StoryPoints","estimateValue":5}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "estimate", Value = "Hours", Number = 2.5m });

        result.IsSuccess.Should().BeTrue();
        After(result, "estimate").Should().Be("2.5 Hours");
        PayloadString(result, "estimateType").Should().Be("Hours");
        result.Payload.RootElement.GetProperty("estimateValue").GetDecimal().Should().Be(2.5m);
    }

    [Fact]
    public async Task Apply_ShouldRefuseAT_ShirtSizeOutsideTheScale()
    {
        var change = Change(
            UpdateTask,
            "Update estimate on “Task”",
            [AiChangeFields.Text("estimate", null, "3 TShirt")],
            """{"taskId":12,"estimateType":"TShirt","estimateValue":3}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "estimate", Value = "TShirt", Number = 6 });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("T-shirt");
    }

    [Fact]
    public async Task Apply_ShouldRefuseAnUnknownPriority()
    {
        var change = Change(
            UpdateTask,
            "Update priority on “Task”",
            [AiChangeFields.Text("priority", "Low", "High")],
            """{"taskId":12,"priority":"High"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "priority", Value = "Urgent" });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Apply_ShouldReplaceTheAssigneesWithThePickedMembers()
    {
        GivenMembers(Member("u1", "Ada"), Member("u2", "Grace"));

        var change = Change(
            AssignTask,
            "Set assignees on “Task”",
            [AiChangeFields.Values("assignees", AiChangeValueKind.User, [], [AiChangeFields.User("u1", "Ada")])],
            """{"taskId":12,"assigneeIds":["u1"]}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "assignees", Values = ["u2"] });

        result.IsSuccess.Should().BeTrue();
        result.Payload.RootElement.GetProperty("assigneeIds").EnumerateArray().Single().GetString().Should().Be("u2");
        Fields(result).Single().AfterValues!.Single().Display.Should().Be("Grace");
    }

    [Fact]
    public async Task Apply_ShouldRefuseAnAssigneeWhoIsNotAMember()
    {
        GivenMembers(Member("u1", "Ada"));

        var change = Change(
            AssignTask,
            "Set assignees on “Task”",
            [AiChangeFields.Values("assignees", AiChangeValueKind.User, [], [AiChangeFields.User("u1", "Ada")])],
            """{"taskId":12,"assigneeIds":["u1"]}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "assignees", Values = ["stranger"] });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Apply_ShouldAcceptATagProposedInTheSameSet()
    {
        GivenTags("bug");

        var tagChange = Change(CreateTag, "Create tag “urgent”", [], """{"name":"urgent"}""");
        var change = Change(
            SetTags,
            "Set tags on “Task”",
            [AiChangeFields.Values("tags", AiChangeValueKind.Tag, [], [AiChangeFields.Tag("bug")])],
            """{"taskId":12,"tags":["bug"]}""");

        var edit = new AiChangeFieldEdit { Name = "tags", Values = ["bug", "urgent"] };
        var result = await Catalog.Apply(change, [tagChange, change], [edit], TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        var tags = result.Payload.RootElement.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString());

        tags.Should().Equal("bug", "urgent");
    }

    [Fact]
    public async Task Apply_ShouldRefuseATagTheWorkspaceDoesNotHave()
    {
        GivenTags("bug");

        var change = Change(
            SetTags,
            "Set tags on “Task”",
            [AiChangeFields.Values("tags", AiChangeValueKind.Tag, [], [AiChangeFields.Tag("bug")])],
            """{"taskId":12,"tags":["bug"]}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "tags", Values = ["invented"] });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("invented");
    }

    [Fact]
    public async Task Apply_ShouldReplaceAPendingSprintWithThePickedOne()
    {
        GivenSprint(new SprintDetailViewModel { Id = 9, Name = "Sprint 9", ProjectId = ProjectId });

        var change = Change(
            CreateTask,
            "Create task “Task”",
            [SprintField([], [AiChangeFields.Sprint(null, "Sprint 1")])],
            """{"name":"Task","projectId":3,"sprintRef":"sprint-1"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "sprint", Value = "9" });

        result.IsSuccess.Should().BeTrue();
        result.Payload.RootElement.GetProperty("sprintId").GetInt32().Should().Be(9);
        result.Payload.RootElement.TryGetProperty("sprintRef", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Apply_ShouldRefuseASprintInAnotherProject()
    {
        GivenSprint(new SprintDetailViewModel { Id = 9, Name = "Sprint 9", ProjectId = 99 });
        GivenTask(new TaskViewModel { Id = TaskId, Name = "Task", ProjectId = ProjectId });

        var change = Change(
            MoveToSprint,
            "Move “Task” into sprint “Sprint 4”",
            [SprintField([], [AiChangeFields.Sprint(4, "Sprint 4")])],
            """{"taskId":12,"sprintId":4}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "sprint", Value = "9" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("different project");
    }

    [Fact]
    public async Task Apply_ShouldRefuseACompletedSprint()
    {
        GivenSprint(new SprintDetailViewModel
        {
            Id = 9,
            Name = "Sprint 9",
            ProjectId = ProjectId,
            Status = SprintStatus.Completed,
        });

        var change = Change(
            MoveToSprint,
            "Move “Task” into sprint “Sprint 4”",
            [SprintField([], [AiChangeFields.Sprint(4, "Sprint 4")])],
            """{"taskId":12,"projectId":3,"sprintId":4}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "sprint", Value = "9" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("completed");
    }

    [Fact]
    public async Task Apply_ShouldRequoteTheSprintAMoveNames()
    {
        GivenSprint(new SprintDetailViewModel { Id = 9, Name = "Sprint 9", ProjectId = ProjectId });

        var change = Change(
            MoveToSprint,
            "Move “Task” into sprint “Sprint 4”",
            [SprintField([], [AiChangeFields.Sprint(4, "Sprint 4")])],
            """{"taskId":12,"projectId":3,"sprintId":4}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "sprint", Value = "9" });

        result.Summary.Should().Be("Move “Task” into sprint “Sprint 9”");
    }

    [Fact]
    public async Task Apply_ShouldMoveATaskToThePickedBoardGroup()
    {
        GivenBoardGroups(BoardGroup(5, "Done", ProjectId));

        var change = Change(
            MoveToBoardGroup,
            "Move “Task” to Doing on Delivery",
            [AiChangeFields.BoardGroup(4, "Delivery · Doing")],
            """{"taskId":12,"projectId":3,"boardGroupId":4,"boardIdentifier":"delivery"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "boardGroup", Value = "5" });

        result.IsSuccess.Should().BeTrue();
        result.Payload.RootElement.GetProperty("boardGroupId").GetInt32().Should().Be(5);
        PayloadString(result, "boardIdentifier").Should().Be("delivery");
        After(result, "boardGroup").Should().Be("Delivery · Done");
        Fields(result).Single().AfterValues!.Single().Id.Should().Be("5");
        result.Summary.Should().Be("Move “Task” to Done on Delivery");
    }

    [Fact]
    public async Task Apply_ShouldRefuseABoardGroupInAnotherProject()
    {
        GivenBoardGroups(BoardGroup(5, "Done", 99));

        var change = Change(
            MoveToBoardGroup,
            "Move “Task” to Doing on Delivery",
            [AiChangeFields.BoardGroup(4, "Delivery · Doing")],
            """{"taskId":12,"projectId":3,"boardGroupId":4,"boardIdentifier":"delivery"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "boardGroup", Value = "5" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("different project");
    }

    [Fact]
    public async Task Apply_ShouldKeepARewrittenMoveSummaryWithinTheColumn()
    {
        var longName = new string('n', 600);

        GivenBoardGroups(BoardGroup(5, longName, ProjectId));

        var change = Change(
            MoveToBoardGroup,
            "Move “Task” to Doing on Delivery",
            [AiChangeFields.BoardGroup(4, "Delivery · Doing")],
            """{"taskId":12,"projectId":3,"boardGroupId":4,"boardIdentifier":"delivery"}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "boardGroup", Value = "5" });

        result.IsSuccess.Should().BeTrue();
        result.Summary.Should().HaveLength(512);
    }

    [Fact]
    public async Task Apply_ShouldSayTheTaskIsGone_WhenTheMovedTaskNoLongerExists()
    {
        GivenSprint(new SprintDetailViewModel { Id = 9, Name = "Sprint 9", ProjectId = ProjectId });

        var change = Change(
            MoveToSprint,
            "Move “Task” into sprint “Sprint 4”",
            [SprintField([], [AiChangeFields.Sprint(4, "Sprint 4")])],
            """{"taskId":12,"sprintId":4}""");

        var result = await Edit(change, new AiChangeFieldEdit { Name = "sprint", Value = "9" });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no longer in this workspace");
    }

    private Task<AiChangeEditResult> Edit(AiProposedChange change, AiChangeFieldEdit edit)
    {
        return Catalog.Apply(change, [change], [edit], TestContext.Current.CancellationToken);
    }

    private void GivenStatuses(params StatusViewModel[] statuses)
    {
        Mediator
            .Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns(statuses.ToList());
    }

    private void GivenMembers(params UserAvatar[] members)
    {
        Mediator
            .Send(Arg.Any<GetWorkspaceMembersByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<GetWorkspaceMembersByIdQuery>();

                return members.Where(member => query.UserIds.Contains(member.Id)).ToList();
            });
    }

    private void GivenTags(params string[] names)
    {
        var tags = names.Select(name => new TagViewModel { Name = name }).ToList();

        Mediator
            .Send(Arg.Any<GetTagsByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns(tags);
    }

    private void GivenSprint(SprintDetailViewModel sprint)
    {
        Mediator
            .Send(Arg.Is<GetSprintQuery>(query => query.Id == sprint.Id), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<SprintDetailViewModel>.Success(sprint));
    }

    private void GivenTask(TaskViewModel task)
    {
        Mediator
            .Send(Arg.Is<GetTaskQuery>(query => query.Id == task.Id), Arg.Any<CancellationToken>())
            .Returns(task);
    }

    private void GivenBoardGroups(params BoardGroupOptionViewModel[] groups)
    {
        Mediator
            .Send(Arg.Any<GetBoardGroupOptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(groups.ToList());
    }

    private static UserAvatar Member(string id, string displayName)
    {
        return new UserAvatar { Id = id, DisplayName = displayName };
    }

    private static BoardGroupOptionViewModel BoardGroup(int id, string name, int projectId)
    {
        return new BoardGroupOptionViewModel
        {
            Id = id,
            Name = name,
            BoardId = 1,
            BoardName = "Delivery",
            BoardIdentifier = "delivery",
            ProjectId = projectId,
            ProjectName = "Web",
        };
    }

    private static AiChangeField StatusField(int beforeId, string beforeName, int afterId, string afterName)
    {
        return AiChangeFields.Values(
            "status",
            AiChangeValueKind.Status,
            [AiChangeFields.Status(beforeId, beforeName)],
            [AiChangeFields.Status(afterId, afterName)]);
    }

    private static AiChangeField SprintField(List<AiChangeValue> before, List<AiChangeValue> after)
    {
        return AiChangeFields.Values("sprint", AiChangeValueKind.Sprint, before, after);
    }

    private static AiProposedChange Change(string toolName, string summary, List<AiChangeField> fields, string payload)
    {
        return new AiProposedChange
        {
            Id = 1,
            ToolName = toolName,
            EntityType = "task",
            Summary = summary,
            Fields = AiChangeFieldSerializer.Serialize(fields),
            Payload = JsonDocument.Parse(payload),
        };
    }

    private static List<AiChangeFieldViewModel> Fields(AiChangeEditResult result)
    {
        return AiChangeFieldSerializer.Deserialize(result.Fields);
    }

    private static string? After(AiChangeEditResult result, string name)
    {
        return Fields(result).Single(field => field.Name == name).After;
    }

    private static string? PayloadString(AiChangeEditResult result, string name)
    {
        return result.Payload.RootElement.GetProperty(name).GetString();
    }
}
