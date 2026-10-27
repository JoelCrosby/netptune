using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Ai.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Ai.Commands;

public class SaveWorkspaceSearchCredentialCommandHandlerTests
{
    private readonly SaveWorkspaceSearchCredentialCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IAiCredentialProtector Protector = Substitute.For<IAiCredentialProtector>();

    public SaveWorkspaceSearchCredentialCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Protector);

        Identity.GetWorkspaceId().Returns(1);
        UnitOfWork.WorkspaceSearchCredentials
            .AddAsync(Arg.Any<WorkspaceSearchCredential>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WorkspaceSearchCredential>());
    }

    [Theory]
    [InlineData("http://169.254.169.254/v1.json#")]
    [InlineData("http://10.0.0.5:6379/?x=")]
    [InlineData("http://localhost:8080")]
    [InlineData("http://searxng.internal/")]
    public async Task Handle_ShouldFail_WhenEndpointIsNotPublic(string endpoint)
    {
        var request = new SaveWorkspaceSearchCredentialRequest
        {
            Provider = WebSearchProvider.Searxng,
            Endpoint = endpoint,
        };

        var result = await Handler.Handle(
            new SaveWorkspaceSearchCredentialCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(AppText.ErrorEndpointMustBePublic);
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenEndpointIsPublic()
    {
        var request = new SaveWorkspaceSearchCredentialRequest
        {
            Provider = WebSearchProvider.Searxng,
            Endpoint = "https://search.example.com",
        };

        var result = await Handler.Handle(
            new SaveWorkspaceSearchCredentialCommand(request),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
