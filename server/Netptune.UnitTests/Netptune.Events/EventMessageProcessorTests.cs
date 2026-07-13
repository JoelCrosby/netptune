using Microsoft.Extensions.Logging;

using NATS.Client.JetStream;

using Netptune.Core.Events;
using Netptune.Events;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Events;

public class EventMessageProcessorTests
{
    private readonly EventRetryPolicy RetryPolicy = new()
    {
        MaxDeliver = 3,
    };

    private readonly EventMessageProcessor Processor;

    private static readonly EventMessage Message = new()
    {
        Type = "Netptune.Core.Events.ActivityMessage",
        Payload = "{}",
    };

    public EventMessageProcessorTests()
    {
        Processor = new(RetryPolicy, Substitute.For<ILogger<EventMessageProcessor>>());
    }

    private static INatsJSMsg<EventMessage> BuildDelivery(int deliveryAttempt)
    {
        var message = Substitute.For<INatsJSMsg<EventMessage>>();

        message.Data.Returns(Message);
        message.Subject.Returns(MessageKeys.Subjects.Activity);
        message.Metadata.Returns(new NatsJSMsgMetadata(
            new NatsJSSequencePair(1, 1),
            (ulong) deliveryAttempt,
            0,
            DateTimeOffset.UtcNow,
            MessageKeys.Queue,
            MessageKeys.Consumers.Activity,
            string.Empty));

        return message;
    }

    [Fact]
    public async Task Process_ShouldAck_WhenHandlerSucceeds()
    {
        var delivery = BuildDelivery(1);

        await Processor.Process(delivery, (_, _) => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        await delivery.Received(1).AckAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().NakAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().AckTerminateAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_ShouldLeaveTheMessageOutstanding_WhenHandlerThrows()
    {
        var delivery = BuildDelivery(1);

        await Processor.Process(delivery, (_, _) => throw new InvalidOperationException("handler failed"), TestContext.Current.CancellationToken);

        await delivery.DidNotReceive().AckAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().NakAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().AckTerminateAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_ShouldTerminate_WhenDeliveriesAreExhausted()
    {
        var delivery = BuildDelivery(3);

        await Processor.Process(delivery, (_, _) => throw new InvalidOperationException("handler failed"), TestContext.Current.CancellationToken);

        await delivery.Received(1).AckTerminateAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());

        await delivery.DidNotReceive().AckAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().NakAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_ShouldTerminate_WhenMessageTypeIsUnknown()
    {
        var delivery = BuildDelivery(1);

        await Processor.Process(delivery, (_, _) => throw new UnknownMessageTypeException(Message.Type), TestContext.Current.CancellationToken);

        await delivery.Received(1).AckTerminateAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());

        await delivery.DidNotReceive().AckAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().NakAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_ShouldTerminate_WhenPayloadIsUnreadable()
    {
        var delivery = Substitute.For<INatsJSMsg<EventMessage>>();

        delivery.Data.Returns((EventMessage?) null);
        delivery.Subject.Returns(MessageKeys.Subjects.Activity);

        await Processor.Process(delivery, (_, _) => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        await delivery.Received(1).AckTerminateAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
        await delivery.DidNotReceive().AckAsync(Arg.Any<AckOpts?>(), Arg.Any<CancellationToken>());
    }
}
