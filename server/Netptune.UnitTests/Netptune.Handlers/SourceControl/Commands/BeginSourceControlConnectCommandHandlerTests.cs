using FluentAssertions;

using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.Handlers.SourceControl.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.SourceControl.Commands;

public class BeginSourceControlConnectCommandHandlerTests
{
    private const int WorkspaceId = 3;
    private const string UserId = "user-1";
    private const SourceControlProvider ProviderKind = SourceControlProvider.AzureDevOps;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly ISourceControlProviderRegistry Registry = Substitute.For<ISourceControlProviderRegistry>();
    private readonly ISourceControlConnector Connector = Substitute.For<ISourceControlConnector>();
    private readonly BeginSourceControlConnectCommandHandler Handler;

    public BeginSourceControlConnectCommandHandlerTests()
    {
        var provider = Substitute.For<ISourceControlProvider>();

        provider.Connector.Returns(Connector);
        Registry.IsEnabled(ProviderKind).Returns(true);
        Registry.Resolve(ProviderKind).Returns(provider);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);
        Handler = new BeginSourceControlConnectCommandHandler(Identity, Registry);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheConnectorsMessage_WhenTheProviderCannotBeginAConnection()
    {
        const string message = "This provider connects with a token.";
        var request = new ScmConnectRequest(WorkspaceId, UserId);
        Connector.BeginConnect(request).Returns(ClientResponse<ScmConnectStart>.Failed(message));

        var command = new BeginSourceControlConnectCommand(new BeginSourceControlConnectRequest { Provider = ProviderKind });
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(message);
        result.Payload.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnTheRedirectUrl_WhenTheProviderBeginsAConnection()
    {
        const string redirectUrl = "https://scm.test/connect";
        var request = new ScmConnectRequest(WorkspaceId, UserId);
        var start = new ScmConnectStart(redirectUrl);
        Connector.BeginConnect(request).Returns(ClientResponse<ScmConnectStart>.Success(start));

        var command = new BeginSourceControlConnectCommand(new BeginSourceControlConnectRequest { Provider = ProviderKind });
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.RedirectUrl.Should().Be(redirectUrl);
    }
}
