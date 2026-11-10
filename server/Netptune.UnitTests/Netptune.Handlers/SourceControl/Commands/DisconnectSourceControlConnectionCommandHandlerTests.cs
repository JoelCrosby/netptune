using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.SourceControl.Commands;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.SourceControl.Commands;

public class DisconnectSourceControlConnectionCommandHandlerTests
{
    private const int WorkspaceId = 3;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly ISourceControlProviderRegistry Registry = Substitute.For<ISourceControlProviderRegistry>();
    private readonly ISourceControlConnector Connector = Substitute.For<ISourceControlConnector>();
    private readonly DisconnectSourceControlConnectionCommandHandler Handler;

    private readonly SourceControlConnection Connection = new()
    {
        Id = 9,
        WorkspaceId = WorkspaceId,
        Provider = SourceControlProvider.GitHub,
        ExternalAccountId = "42",
        AccountLogin = "octocat",
        Credential = [1, 2, 3],
        WebhookSecret = [4, 5, 6],
    };

    public DisconnectSourceControlConnectionCommandHandlerTests()
    {
        var provider = Substitute.For<ISourceControlProvider>();

        provider.Connector.Returns(Connector);
        Registry.IsEnabled(SourceControlProvider.GitHub).Returns(true);
        Registry.Resolve(SourceControlProvider.GitHub).Returns(provider);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns("user-1");
        UnitOfWork.SourceControlConnections
            .GetOwned(Connection.Id, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Connection);
        UnitOfWork.SourceControlRepos
            .GetForConnection(Connection.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        UnitOfWork.InvokeTransaction();

        Handler = new DisconnectSourceControlConnectionCommandHandler(
            UnitOfWork,
            Identity,
            EventRecords,
            Registry,
            NullLogger<DisconnectSourceControlConnectionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldDisconnectFromTheProviderAfterCommitting()
    {
        var committedFirst = false;
        ScmConnection? disconnected = null;

        Connector
            .Disconnect(Arg.Any<ScmConnection>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                committedFirst = UnitOfWork.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "CompleteAsync");
                disconnected = callInfo.Arg<ScmConnection>();

                return ValueTask.CompletedTask;
            });

        var command = new DisconnectSourceControlConnectionCommand(Connection.Id);
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        committedFirst.Should().BeTrue();
        disconnected!.ProtectedCredential.Should().NotBeNull();
        disconnected.ProtectedWebhookSecret.Should().NotBeNull();
        Connection.Credential.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldStillDisconnect_WhenTheProviderFails()
    {
        Connector
            .Disconnect(Arg.Any<ScmConnection>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("unreachable"));

        var command = new DisconnectSourceControlConnectionCommand(Connection.Id);
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        Connection.IsDeleted.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
