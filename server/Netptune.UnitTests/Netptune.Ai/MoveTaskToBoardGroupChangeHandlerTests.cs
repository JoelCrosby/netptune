using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class MoveTaskToBoardGroupChangeHandlerTests
{
    private const int TaskId = 42;
    private const int BoardGroupId = 29;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Apply_ShouldMoveThroughTheBoardAwareCommand_SoATaskCanJoinAnotherBoard()
    {
        Mediator
            .Send(Arg.Any<MoveTasksToBoardGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);

        var handler = new MoveTaskToBoardGroupChangeHandler(Mediator);
        var context = CreateContext(new { taskId = TaskId, boardGroupId = BoardGroupId, boardIdentifier = "slash-commands" });
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<MoveTasksToBoardGroupCommand>(command =>
                command.BoardGroupId == BoardGroupId &&
                command.TaskIds.SequenceEqual(new[] { TaskId })),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<MoveTasksToGroupCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_ShouldFail_WhenTheMoveIsRejected()
    {
        Mediator
            .Send(Arg.Any<MoveTasksToBoardGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Failed("Board group “Backlog” belongs to a different project."));

        var handler = new MoveTaskToBoardGroupChangeHandler(Mediator);
        var context = CreateContext(new { taskId = TaskId, boardGroupId = BoardGroupId });
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
    }

    [Fact]
    public async Task Apply_ShouldFail_WhenThePayloadHasNoBoardGroup()
    {
        var handler = new MoveTaskToBoardGroupChangeHandler(Mediator);
        var context = CreateContext(new { taskId = TaskId });
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        await Mediator.DidNotReceive().Send(Arg.Any<MoveTasksToBoardGroupCommand>(), Arg.Any<CancellationToken>());
    }

    private static AiChangeApplyContext CreateContext(object payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = "propose_move_task_to_board_group",
            EntityType = "task",
            EntityId = TaskId,
            Summary = "Move a task",
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
