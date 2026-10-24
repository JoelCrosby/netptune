using System.Globalization;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.Services.Localization;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;
using Netptune.JobServer.Handlers;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Messages;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.JobServer;

public class ExportJobHandlerTests
{
    private const string UserId = "user-1";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IExportJobRepository ExportJobs = Substitute.For<IExportJobRepository>();
    private readonly IExportRunner Runner = Substitute.For<IExportRunner>();
    private readonly IUserLocaleReader Locales = Substitute.For<IUserLocaleReader>();
    private readonly ExportJobHandler Handler;

    public ExportJobHandlerTests()
    {
        Handler = new ExportJobHandler(
            UnitOfWork,
            Runner,
            Substitute.For<IStorageService>(),
            Substitute.For<ITransferJobNotifier>(),
            Substitute.For<IEventRecordWriter>(),
            Substitute.For<INotificationDispatcher>(),
            Substitute.For<IActorContext>(),
            Locales,
            NullLogger<ExportJobHandler>.Instance,
            ExportJobs);
    }

    [Fact]
    public async Task Handle_ShouldRunTheExportInTheRequestersLanguage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runCulture = string.Empty;

        ExportJobs.GetForProcessing(7, cancellationToken).Returns(Job());
        Locales.GetLocale(UserId, cancellationToken).Returns("fr");
        Runner
            .Run(Arg.Any<ExportRunRequest>(), Arg.Any<ExportProgressReporter>(), cancellationToken)
            .Returns<Task<ExportRunResult>>(_ =>
            {
                runCulture = CultureInfo.CurrentUICulture.Name;

                throw new OperationCanceledException();
            });

        var handle = async () => await Handler.Handle(Message(), cancellationToken);

        await handle.Should().ThrowAsync<OperationCanceledException>();

        runCulture.Should().Be("fr");
    }

    private static ExportJobRequestedMessage Message()
    {
        return new ExportJobRequestedMessage
        {
            WorkspaceId = 3,
            ExportJobId = 7,
            UserId = UserId,
        };
    }

    private static ExportJob Job()
    {
        return new ExportJob
        {
            Id = 7,
            WorkspaceId = 3,
            Workspace = new Workspace { Id = 3, Slug = "acme", Name = "Acme" },
            RecordType = "task",
            Definition = JsonDocument.Parse("""{"recordType":"task","format":0}"""),
            RequestedBy = UserId,
        };
    }
}
