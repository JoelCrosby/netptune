using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Repositories;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Activity;
using Netptune.Services.Ai;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Activity;

public class EventRecordWriterAttributionTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IEventRecordRepository EventRecords = Substitute.For<IEventRecordRepository>();
    private readonly AiExecutionContext AiExecution = new();

    public EventRecordWriterAttributionTests()
    {
        UnitOfWork.EventRecords.Returns(EventRecords);

        EventRecords
            .AppendAsync(Arg.Any<EventRecord>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<EventRecord>()));
    }

    [Fact]
    public async Task Append_ShouldAttributeToTheUser_WhenNoAssistantScopeIsActive()
    {
        var writer = CreateWriter();
        var record = await writer.Append(CreateRequest(), TestContext.Current.CancellationToken);

        record.OriginType.Should().Be(EventOriginType.User);
        record.Agent.Should().BeNull();
    }

    [Fact]
    public async Task Append_ShouldAttributeToTheAssistant_WhenTheScopeIsActive()
    {
        var correlationId = Guid.NewGuid();
        var writer = CreateWriter();

        using (AiExecution.Begin("claude-opus-5", correlationId))
        {
            var record = await writer.Append(CreateRequest(), TestContext.Current.CancellationToken);

            record.OriginType.Should().Be(EventOriginType.Assistant);
            record.Agent.Should().Be("claude-opus-5");
            record.CorrelationId.Should().Be(correlationId);
        }
    }

    [Fact]
    public async Task Append_ShouldStopAttributingToTheAssistant_WhenTheScopeEnds()
    {
        var writer = CreateWriter();

        using (AiExecution.Begin("claude-opus-5", Guid.NewGuid()))
        {
            await writer.Append(CreateRequest(), TestContext.Current.CancellationToken);
        }

        var record = await writer.Append(CreateRequest(), TestContext.Current.CancellationToken);

        record.OriginType.Should().Be(EventOriginType.User);
        record.Agent.Should().BeNull();
    }

    private EventRecordWriter CreateWriter()
    {
        return new EventRecordWriter(UnitOfWork, aiExecution: AiExecution);
    }

    private static EventWriteRequest<EntityCreatedPayload> CreateRequest()
    {
        return new EventWriteRequest<EntityCreatedPayload>
        {
            WorkspaceId = 1,
            EventKey = EventKeys.EntityCreated,
            SubjectType = "task",
            SubjectId = "42",
            Payload = new EntityCreatedPayload { Name = "Test" },
        };
    }
}
