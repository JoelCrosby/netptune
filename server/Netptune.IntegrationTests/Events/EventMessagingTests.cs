using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Mediator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

using Netptune.Core.Events;
using Netptune.Core.Services.Activity;
using Netptune.Events;
using Netptune.Events.Configuration;

using NSubstitute;

using Xunit;

namespace Netptune.IntegrationTests.Events;

public class EventMessagingTests(NatsEventsFixture fixture) : IClassFixture<NatsEventsFixture>, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private const int TestMaxDeliver = 3;

    // Backoff is [500ms, 1s], so a message that always throws exhausts its three deliveries in a second.
    private static readonly EventRetryPolicy TestRetryPolicy = new()
    {
        AckWait = TimeSpan.FromMilliseconds(500),
        MaxDeliver = TestMaxDeliver,
        RetrySteps = [2],
    };

    private const string TerminatedAdvisorySubject = "$JS.EVENT.ADVISORY.CONSUMER.MSG_TERMINATED.>";

    private readonly ConcurrentBag<object> Handled = [];

    private readonly ConcurrentBag<DateTime> Deliveries = [];

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await using var provider = BuildProvider();

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        try
        {
            await jetStream.DeleteStreamAsync(MessageKeys.Queue, CancellationToken);
        }
        catch (NatsJSApiException exception) when (exception.Error.Code is 404)
        {
            // stream does not exist yet.
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Consumer_ShouldAckAndNotRedeliver_WhenHandlerSucceeds()
    {
        await using var provider = BuildProvider();

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var consumer = await StartConsumer(provider);

        await PublishActivity(jetStream, consumer.Task);

        await WaitUntil(() => Handled.Count is 1, consumer.Task, "the message to be handled");

        // longer than AckWait — a message that was not acked would be redelivered inside this window.
        await Task.Delay(TestRetryPolicy.AckWait + TimeSpan.FromSeconds(1), CancellationToken);

        Handled.Should().HaveCount(1);

        var info = await GetConsumer(jetStream, MessageKeys.Consumers.Activity);

        info.NumPending.Should().Be(0);
        info.NumAckPending.Should().Be(0);
        info.NumRedelivered.Should().Be(0);
        info.Delivered.StreamSeq.Should().Be(1);
    }

    [Fact]
    public async Task Consumer_ShouldRedeliverOnTheServersSchedule_WhenHandlerThrows()
    {
        // A long second delay, so the message is still mid-retry when its state is read below. On the
        // default test schedule it would burn all three deliveries in a second and the assertions would
        // race it.
        var policy = TestRetryPolicy with
        {
            AckWait = TimeSpan.FromSeconds(1),
            RetrySteps = [20],
        };

        await using var provider = BuildProvider(
            () => throw new InvalidOperationException("handler failed"),
            policy);

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var consumer = await StartConsumer(provider);

        await PublishActivity(jetStream, consumer.Task);

        await WaitUntil(() => Deliveries.Count >= 2, consumer.Task, "the message to be redelivered");

        var info = await GetConsumer(jetStream, MessageKeys.Consumers.Activity);

        info.NumRedelivered.Should().BeGreaterThanOrEqualTo(1);
        (info.NumAckPending + (long) info.NumPending).Should().BeGreaterThanOrEqualTo(1);

        var ordered = Deliveries.OrderBy(delivery => delivery).ToList();

        (ordered[1] - ordered[0]).Should().BeGreaterThanOrEqualTo(policy.Backoff[0]);

        info.Config.Backoff.Should().BeEquivalentTo(policy.Backoff);
        info.Config.MaxDeliver.Should().Be(TestMaxDeliver);
    }

    [Fact]
    public async Task Consumer_ShouldTerminateAndNotAck_WhenDeliveriesAreExhausted()
    {
        await using var provider = BuildProvider(() => throw new InvalidOperationException("handler failed"));

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var advisories = await SubscribeToTerminatedAdvisories(provider);
        await using var consumer = await StartConsumer(provider);

        await PublishActivity(jetStream, consumer.Task);

        await WaitUntil(() => advisories.Received.Count is 1, consumer.Task, "the terminated advisory");

        Deliveries.Should().HaveCount(TestMaxDeliver);

        var advisory = advisories.Received.Single();

        advisory.Should().Contain("io.nats.jetstream.advisory.v1.terminated");
        advisory.Should().Contain($"\"consumer\":\"{MessageKeys.Consumers.Activity}\"");
        advisory.Should().Contain("\"stream_seq\":1");
        advisory.Should().Contain($"\"deliveries\":{TestMaxDeliver}");

        await Task.Delay(TestRetryPolicy.AckWait + TimeSpan.FromSeconds(1), CancellationToken);

        Deliveries.Should().HaveCount(TestMaxDeliver);

        var info = await GetConsumer(jetStream, MessageKeys.Consumers.Activity);

        info.NumAckPending.Should().Be(0);
        info.NumPending.Should().Be(0);

        (await ReadSubject(jetStream, MessageKeys.Subjects.Activity)).Should().ContainSingle();
    }

    [Fact]
    public async Task Consumer_ShouldTerminateAndNotAck_WhenMessageTypeIsUnknown()
    {
        await using var provider = BuildProvider();

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var advisories = await SubscribeToTerminatedAdvisories(provider);
        await using var consumer = await StartConsumer(provider);

        await WaitForConsumer(jetStream, MessageKeys.Consumers.Activity, consumer.Task);

        await jetStream.PublishAsync(
            MessageKeys.Subjects.Activity,
            new EventMessage { Type = "Netptune.Core.Events.NotARealType", Payload = "{}" },
            cancellationToken: CancellationToken);

        await WaitUntil(() => advisories.Received.Count is 1, consumer.Task, "the terminated advisory");

        var advisory = advisories.Received.Single();

        advisory.Should().Contain("io.nats.jetstream.advisory.v1.terminated");

        advisory.Should().Contain("\"deliveries\":1");

        await Task.Delay(TestRetryPolicy.AckWait + TimeSpan.FromSeconds(1), CancellationToken);

        advisories.Received.Should().ContainSingle();
        Handled.Should().BeEmpty();

        var info = await GetConsumer(jetStream, MessageKeys.Consumers.Activity);

        info.NumAckPending.Should().Be(0);
        info.NumPending.Should().Be(0);

        (await ReadSubject(jetStream, MessageKeys.Subjects.Activity)).Should().ContainSingle();
    }

    [Fact]
    public async Task Consumer_ShouldOnlyReceiveItsOwnSubject_WhenOtherSubjectsArePublished()
    {
        await using var provider = BuildProvider();

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var consumer = await StartConsumer(provider);

        await WaitForConsumer(jetStream, MessageKeys.Consumers.Activity, consumer.Task);

        foreach (var subject in new[]
                 {
                     MessageKeys.Subjects.Search,
                     MessageKeys.Subjects.Email,
                     MessageKeys.Subjects.Automation,
                 })
        {
            await jetStream.PublishAsync(subject, BuildMessage(subject), cancellationToken: CancellationToken);
        }

        await jetStream.PublishAsync(
            MessageKeys.Subjects.Activity,
            BuildMessage(MessageKeys.Subjects.Activity),
            cancellationToken: CancellationToken);

        await WaitUntil(() => Handled.Count >= 1, consumer.Task, "the activity message to be handled");

        // give the other three subjects every chance to leak through.
        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken);

        HandledTags().Should().BeEquivalentTo([MessageKeys.Subjects.Activity]);

        var info = await GetConsumer(jetStream, MessageKeys.Consumers.Activity);

        info.NumPending.Should().Be(0);
        info.NumAckPending.Should().Be(0);

        var stream = await jetStream.GetStreamAsync(MessageKeys.Queue, cancellationToken: CancellationToken);

        stream.Info.State.Messages.Should().Be(4);
    }

    [Fact]
    public async Task Publisher_ShouldPublishToTheSubjectTheMessageDeclares()
    {
        await using var provider = BuildProvider();

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var consumer = await StartConsumer(provider);

        await WaitForConsumer(jetStream, MessageKeys.Consumers.Activity, consumer.Task);

        await provider.GetRequiredService<IEventPublisher>().Dispatch(new TestPayload());
        await provider.GetRequiredService<IEventPublisher>().Dispatch(new TestSearchPayload());

        await WaitUntil(() => Handled.Count >= 1, consumer.Task, "the activity message to be handled");

        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken);

        Handled.Should().ContainSingle().Which.Should().BeOfType<TestPayload>();

        (await ReadSubject(jetStream, MessageKeys.Subjects.Search)).Should().ContainSingle()
            .Which.Data!.Type.Should().Be(typeof(TestSearchPayload).FullName);

        var stream = await jetStream.GetStreamAsync(MessageKeys.Queue, cancellationToken: CancellationToken);

        stream.Info.Config.Subjects.Should().BeEquivalentTo([MessageKeys.Subjects.Typed]);
    }

    [Fact]
    public async Task JobsConsumer_ShouldTakeItsThreeSubjects_AndNothingElse()
    {
        await using var provider = BuildJobServerProvider();

        var jetStream = provider.GetRequiredService<INatsJSContext>();

        await using var consumer = await StartConsumer(provider);

        await WaitForConsumer(jetStream, MessageKeys.Consumers.Jobs, consumer.Task);

        foreach (var subject in new[]
                 {
                     MessageKeys.Subjects.Search,
                     MessageKeys.Subjects.Email,
                     MessageKeys.Subjects.Automation,

                     MessageKeys.Subjects.Activity,
                 })
        {
            await jetStream.PublishAsync(subject, BuildMessage(subject), cancellationToken: CancellationToken);
        }

        await WaitUntil(() => Handled.Count >= 3, consumer.Task, "the consumer to handle its own subjects");

        // give the activity subject every chance to leak in.
        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken);

        HandledTags().Should().BeEquivalentTo([
            MessageKeys.Subjects.Search,
            MessageKeys.Subjects.Email,
            MessageKeys.Subjects.Automation,
        ]);

        var jobs = await GetConsumer(jetStream, MessageKeys.Consumers.Jobs);

        jobs.Config.FilterSubjects.Should().BeEquivalentTo([
            MessageKeys.Subjects.Search,
            MessageKeys.Subjects.Email,
            MessageKeys.Subjects.Automation,
        ]);

        jobs.NumPending.Should().Be(0);
        jobs.NumAckPending.Should().Be(0);

        (await ReadSubject(jetStream, MessageKeys.Subjects.Activity)).Should().ContainSingle();
    }

    private ServiceProvider BuildProvider(
        Action? onHandle = null,
        EventRetryPolicy? retryPolicy = null,
        Action<EventConsumerOptions>? configure = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // ahead of AddNetptuneMessageQueue, which only supplies the production policy if nothing else has.
        services.AddSingleton(retryPolicy ?? TestRetryPolicy);
        services.AddSingleton(BuildMediator(onHandle));

        services.AddNetptuneMessageQueue(fixture.ConnectionString, options =>
        {
            options.DurableName = MessageKeys.Consumers.Activity;
            options.FilterSubjects = [MessageKeys.Subjects.Activity];

            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    private ServiceProvider BuildJobServerProvider()
    {
        return BuildProvider(configure: options =>
        {
            options.DurableName = MessageKeys.Consumers.Jobs;
            options.FilterSubjects =
            [
                MessageKeys.Subjects.Search,
                MessageKeys.Subjects.Email,
                MessageKeys.Subjects.Automation,
            ];
        });
    }

    private IMediator BuildMediator(Action? onHandle)
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Deliveries.Add(DateTime.UtcNow);

            onHandle?.Invoke();

            Handled.Add(call.Arg<object>());

            return ValueTask.FromResult<object?>(null);
        });

        return mediator;
    }

    private List<string> HandledTags()
    {
        return Handled.OfType<TestPayload>().Select(payload => payload.Tag).ToList();
    }

    private static EventMessage BuildMessage(string tag) => new()
    {
        Type = typeof(TestPayload).FullName!,
        Payload = JsonSerializer.Serialize(new TestPayload { Tag = tag }),
    };

    private async Task<ConsumerHarness> StartConsumer(ServiceProvider provider)
    {
        var service = provider.GetServices<IHostedService>().OfType<EventConsumerService>().Single();

        await service.StartAsync(CancellationToken);

        return new (service);
    }

    // A core NATS subscription, not a JetStream one: advisories are published on the system's own subjects
    // and are not part of the netptune-events stream.
    private async Task<AdvisoryHarness> SubscribeToTerminatedAdvisories(ServiceProvider provider)
    {
        var connection = provider.GetRequiredService<INatsConnection>();
        var harness = new AdvisoryHarness(CancellationToken);

        var subscribed = new TaskCompletionSource();

        harness.Task = Task.Run(
            async () =>
            {
                var messages = connection.SubscribeAsync(
                    TerminatedAdvisorySubject,
                    serializer: NatsRawSerializer<byte[]>.Default,
                    cancellationToken: harness.Cancellation.Token);

                await using var enumerator = messages.GetAsyncEnumerator(harness.Cancellation.Token);

                var moveNext = enumerator.MoveNextAsync();

                subscribed.SetResult();

                while (await moveNext)
                {
                    harness.Received.Add(Encoding.UTF8.GetString(enumerator.Current.Data ?? []));

                    moveNext = enumerator.MoveNextAsync();
                }
            },
            harness.Cancellation.Token);

        await subscribed.Task;

        // an advisory is fire-and-forget, so the subscription must have landed on the server before the
        // caller publishes anything. This is what waits for that.
        await connection.PingAsync(CancellationToken);

        return harness;
    }

    private async Task PublishActivity(INatsJSContext jetStream, Task consumerTask)
    {
        await WaitForConsumer(jetStream, MessageKeys.Consumers.Activity, consumerTask);

        await jetStream.PublishAsync(
            MessageKeys.Subjects.Activity,
            BuildMessage("activity"),
            cancellationToken: CancellationToken);
    }

    private async Task<ConsumerInfo> GetConsumer(INatsJSContext jetStream, string durable)
    {
        var consumer = await jetStream.GetConsumerAsync(MessageKeys.Queue, durable, CancellationToken);

        return consumer.Info;
    }

    private async Task<ConsumerInfo> WaitForConsumer(INatsJSContext jetStream, string durable, Task consumerTask)
    {
        ConsumerInfo? info = null;

        await WaitUntil(
            async () =>
            {
                try
                {
                    info = await GetConsumer(jetStream, durable);

                    return true;
                }
                catch (NatsJSApiException)
                {
                    return false;
                }
            },
            consumerTask,
            $"consumer {durable} to exist");

        return info!;
    }

    private async Task<List<INatsJSMsg<EventMessage>>> ReadSubject(INatsJSContext jetStream, string subject)
    {
        var consumer = await jetStream.CreateOrderedConsumerAsync(
            MessageKeys.Queue,
            new NatsJSOrderedConsumerOpts { FilterSubjects = [subject] },
            CancellationToken);

        var messages = new List<INatsJSMsg<EventMessage>>();

        var options = new NatsJSFetchOpts
        {
            MaxMsgs = 16,
            Expires = TimeSpan.FromSeconds(2),
        };

        await foreach (var message in consumer.FetchNoWaitAsync<EventMessage>(options, cancellationToken: CancellationToken))
        {
            messages.Add(message);
        }

        return messages;
    }

    private Task WaitUntil(Func<bool> condition, Task consumerTask, string because)
    {
        return WaitUntil(() => Task.FromResult(condition()), consumerTask, because);
    }

    private async Task WaitUntil(Func<Task<bool>> condition, Task consumerTask, string because)
    {
        var deadline = DateTime.UtcNow.Add(Timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (consumerTask.IsFaulted) await consumerTask;

            if (await condition()) return;

            await Task.Delay(100, CancellationToken);
        }

        throw new TimeoutException($"Timed out after {Timeout} waiting for {because}.");
    }

    private sealed class ConsumerHarness(EventConsumerService service) : IAsyncDisposable
    {
        public Task Task => service.ExecuteTask ?? Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private sealed class AdvisoryHarness(CancellationToken cancellationToken) : IAsyncDisposable
    {
        public CancellationTokenSource Cancellation { get; } = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        public ConcurrentBag<string> Received { get; } = [];

        public Task Task { get; set; } = Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            await Cancellation.CancelAsync();

            try
            {
                await Task;
            }
            catch (OperationCanceledException)
            {
                // expected — the subscription runs until cancelled.
            }

            Cancellation.Dispose();
        }
    }

    private sealed record TestPayload : IEventMessage
    {
        public string Tag { get; init; } = string.Empty;

        public static string Subject => MessageKeys.Subjects.Activity;
    }

    private sealed record TestSearchPayload : IEventMessage
    {
        public static string Subject => MessageKeys.Subjects.Search;
    }
}
