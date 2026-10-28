using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class RemoveTaskFromBoardChangeHandlerTests
{
    private const int TaskId = 42;
    private const int BoardId = 5;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Apply_ShouldTakeTheTaskOffTheBoard()
    {
        Mediator
            .Send(Arg.Any<RemoveTaskFromBoardCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = TaskId }));

        var handler = new RemoveTaskFromBoardChangeHandler(Mediator);
        var context = CreateContext(new { taskId = TaskId, boardId = BoardId });
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<RemoveTaskFromBoardCommand>(command => command.TaskId == TaskId && command.BoardId == BoardId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_ShouldFail_WhenTheRemovalIsRejected()
    {
        Mediator
            .Send(Arg.Any<RemoveTaskFromBoardCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Failed("The task is not on board 'Netptune'"));

        var handler = new RemoveTaskFromBoardChangeHandler(Mediator);
        var context = CreateContext(new { taskId = TaskId, boardId = BoardId });
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
    }

    [Fact]
    public async Task Apply_ShouldFail_WhenThePayloadHasNoBoard()
    {
        var handler = new RemoveTaskFromBoardChangeHandler(Mediator);
        var context = CreateContext(new { taskId = TaskId });
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        await Mediator.DidNotReceive().Send(Arg.Any<RemoveTaskFromBoardCommand>(), Arg.Any<CancellationToken>());
    }

    private static AiChangeApplyContext CreateContext(object payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = "propose_remove_task_from_board",
            EntityType = "task",
            EntityId = TaskId,
            Summary = "Take a task off a board",
            Payload = JsonSerializer.SerializeToDocument(payload),
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
