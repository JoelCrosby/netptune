using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

// The assistant writes markdown, the editor reads its own document format, and the handlers are
// where the two meet: nothing between the proposal and the database converts the value.
public class TaskDescriptionChangeHandlerTests
{
    private const int TaskId = 42;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Apply_ShouldWriteACreatedDescriptionAsAnEditorDocument()
    {
        var payload = JsonSerializer.Serialize(new
        {
            name = "Fix the login page",
            projectId = 3,
            description = "## Steps\n\nRun **the** thing.",
        });

        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = TaskId }));

        var handler = new CreateTaskChangeHandler(Mediator);
        var context = CreateContext("propose_create_task", payload);
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var written = CapturedCreate();

        written.Should().Contain("\"header\"");
        written.Should().Contain("<b>the</b>");
        written.Should().NotContain("##");
    }

    [Fact]
    public async Task Apply_ShouldWriteAnUpdatedDescriptionAsAnEditorDocument()
    {
        var payload = JsonSerializer.Serialize(new
        {
            taskId = TaskId,
            description = "- one\n- two",
        });

        Mediator
            .Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = TaskId }));

        var handler = new UpdateTaskChangeHandler(Mediator);
        var context = CreateContext("propose_update_task", payload);
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var written = CapturedUpdate();

        written.Should().Contain("\"list\"");
        written.Should().Contain("\"unordered\"");
    }

    [Fact]
    public async Task Apply_ShouldLeaveTheDescriptionAlone_WhenTheChangeDoesNotTouchIt()
    {
        var payload = JsonSerializer.Serialize(new { taskId = TaskId, name = "A new name" });

        Mediator
            .Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = TaskId }));

        var handler = new UpdateTaskChangeHandler(Mediator);
        var context = CreateContext("propose_update_task", payload);

        await handler.Apply(context, TestContext.Current.CancellationToken);

        CapturedUpdate().Should().BeNull();
    }

    private string? CapturedCreate()
    {
        var call = Mediator.ReceivedCalls().Single(call => call.GetArguments()[0] is CreateTaskCommand);
        var command = (CreateTaskCommand)call.GetArguments()[0]!;

        return command.Request.Description;
    }

    private string? CapturedUpdate()
    {
        var call = Mediator.ReceivedCalls().Single(call => call.GetArguments()[0] is UpdateTaskCommand);
        var command = (UpdateTaskCommand)call.GetArguments()[0]!;

        return command.Request.Description;
    }

    private static AiChangeApplyContext CreateContext(string toolName, string payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = toolName,
            EntityType = "task",
            EntityId = TaskId,
            Summary = "Change a description",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };

        return new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal),
        };
    }
}
