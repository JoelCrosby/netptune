using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Core.Repositories;
using Netptune.Core.Services.Activity;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.UnitOfWork;
using Netptune.SourceControl.Webhooks;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl.Webhooks;

public sealed class ScmWebhookMaintenanceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IScmWebhookDeliveryRepository Deliveries = Substitute.For<IScmWebhookDeliveryRepository>();
    private readonly ISourceControlCheckRunRepository CheckRuns = Substitute.For<ISourceControlCheckRunRepository>();
    private readonly IEventPublisher Publisher = Substitute.For<IEventPublisher>();
    private readonly ScmWebhookMaintenanceService Service;

    public ScmWebhookMaintenanceServiceTests()
    {
        UnitOfWork.ScmWebhookDeliveries.Returns(Deliveries);
        UnitOfWork.SourceControlCheckRuns.Returns(CheckRuns);

        var services = new ServiceCollection()
            .AddSingleton(UnitOfWork)
            .AddSingleton(Publisher)
            .BuildServiceProvider();

        Service = new ScmWebhookMaintenanceService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new AdjustableTimeProvider(Now),
            NullLogger<ScmWebhookMaintenanceService>.Instance);
    }

    [Fact]
    public async Task RunOnce_ShouldRepublishStalledDeliveries()
    {
        Deliveries
            .GetStalled(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([11L, 12L]);

        await Service.RunOnce(TestContext.Current.CancellationToken);

        await Publisher.Received(1).Dispatch(ForDelivery(11));
        await Publisher.Received(1).Dispatch(ForDelivery(12));
    }

    [Fact]
    public async Task RunOnce_ShouldPruneProcessedAndAbandonedDeliveriesAndOldCheckRuns()
    {
        Deliveries
            .GetStalled(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await Service.RunOnce(TestContext.Current.CancellationToken);

        var retentionCutoff = Now.UtcDateTime.AddDays(-14);

        await Deliveries.Received(1).DeleteProcessedBefore(retentionCutoff, Arg.Any<CancellationToken>());
        await Deliveries.Received(1).DeleteAbandonedBefore(retentionCutoff, 5, Arg.Any<CancellationToken>());
        await CheckRuns.Received(1).DeleteUpdatedBefore(Now.UtcDateTime.AddDays(-90), Arg.Any<CancellationToken>());
    }

    private static ScmWebhookReceivedMessage ForDelivery(long deliveryRecordId)
    {
        return Arg.Is<ScmWebhookReceivedMessage>(message => message.DeliveryRecordId == deliveryRecordId);
    }
}
