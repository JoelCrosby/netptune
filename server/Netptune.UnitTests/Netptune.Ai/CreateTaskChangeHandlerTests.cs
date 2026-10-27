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

public class CreateTaskChangeHandlerTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Apply_ShouldCreateTheTaskWithItsTags_InOneCommand()
    {
        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = 12 }));

        var handler = new CreateTaskChangeHandler(Mediator);
        var context = CreateContext("""{"name":"Draft the brief","projectId":1,"tags":["Backend","Docs"]}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        result.AppliedEntityId.Should().Be(12);
        await Mediator.Received(1).Send(
            Arg.Is<CreateTaskCommand>(command => command.Request.Tags!.SequenceEqual(new[] { "Backend", "Docs" })),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_ShouldFailWithoutAnEntity_WhenTheCreateIsRefused()
    {
        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Failed("Tags were not found in the workspace: Gone."));

        var handler = new CreateTaskChangeHandler(Mediator);
        var context = CreateContext("""{"name":"Draft the brief","projectId":1,"tags":["Gone"]}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.AppliedEntityId.Should().BeNull();
    }

    private static AiChangeApplyContext CreateContext(string payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = "propose_create_task",
            EntityType = "task",
            Summary = "Create a task",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };

        return new AiChangeApplyContext { Change = change, ResolvedRefs = new Dictionary<string, int>() };
    }
}
