using FluentAssertions;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Execution;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiCredentialResolverTests
{
    private const int WorkspaceId = 7;
    private const string UserId = "user-1";

    private static readonly byte[] ProtectedDevelopmentKey = [9, 9, 9];

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IAiCredentialProtector Protector = Substitute.For<IAiCredentialProtector>();
    private readonly UserAiCredential UserCredential = new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        Provider = AiProvider.Anthropic,
        Label = "Mine",
        Secret = [1, 2, 3],
        SecretHint = "…abc",
    };
    private readonly WorkspaceAiCredential WorkspaceCredential = new()
    {
        Id = Guid.NewGuid(),
        WorkspaceId = WorkspaceId,
        Provider = AiProvider.OpenAi,
        Label = "Shared",
        Secret = [4, 5, 6],
        SecretHint = "…def",
    };

    public AiCredentialResolverTests()
    {
        UnitOfWork.AiCredentials.GetForUser(UserId, Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(WorkspaceId, Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.AiCredentials
            .GetAsync(UserCredential.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(UserCredential);
        UnitOfWork.WorkspaceAiCredentials
            .GetAsync(WorkspaceCredential.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(WorkspaceCredential);

        Protector.Protect(Arg.Any<string>()).Returns(ProtectedDevelopmentKey);
    }

    [Fact]
    public async Task Resolve_ShouldUseTheTrimmedDevelopmentKey_WhenNoConnectionIsStored()
    {
        var resolver = CreateResolver(new AiDevelopmentKeys { Anthropic = "  sk-dev  " });

        var credentials = await resolver.Resolve(UserId, WorkspaceId, TestContext.Current.CancellationToken);

        var credential = credentials.Should().ContainSingle().Subject;

        credential.Provider.Should().Be(AiProvider.Anthropic);
        credential.Source.Should().Be(AiCredentialSource.Development);
        credential.Secret.Should().Equal(ProtectedDevelopmentKey);
        Protector.Received(1).Protect("sk-dev");
    }

    [Fact]
    public async Task Resolve_ShouldPreferStoredConnections_AndFillOnlyTheMissingProvider()
    {
        UnitOfWork.AiCredentials.GetForUser(UserId, Arg.Any<CancellationToken>()).Returns([UserCredential]);

        var resolver = CreateResolver(new AiDevelopmentKeys { Anthropic = "sk-dev", OpenAi = "sk-dev-openai" });

        var credentials = await resolver.Resolve(UserId, WorkspaceId, TestContext.Current.CancellationToken);

        credentials.Select(credential => (credential.Provider, credential.Source)).Should().Equal(
            (AiProvider.Anthropic, AiCredentialSource.User),
            (AiProvider.OpenAi, AiCredentialSource.Development));
        credentials[0].Secret.Should().Equal(UserCredential.Secret);
    }

    [Fact]
    public async Task Resolve_ShouldIgnoreBlankDevelopmentKeys()
    {
        var resolver = CreateResolver(new AiDevelopmentKeys { Anthropic = "   ", OpenAi = "" });

        var credentials = await resolver.Resolve(UserId, WorkspaceId, TestContext.Current.CancellationToken);

        credentials.Should().BeEmpty();
        Protector.DidNotReceive().Protect(Arg.Any<string>());
    }

    [Fact]
    public async Task MarkUsed_ShouldNotTouchStoredConnections_WhenTheKeyIsADevelopmentKey()
    {
        var resolver = CreateResolver(new AiDevelopmentKeys { Anthropic = "sk-dev" });
        var credentials = await resolver.Resolve(UserId, WorkspaceId, TestContext.Current.CancellationToken);

        await resolver.MarkUsed(credentials.Single(), TestContext.Current.CancellationToken);

        await UnitOfWork.AiCredentials.DidNotReceive().GetAsync(
            Arg.Any<Guid>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await UnitOfWork.WorkspaceAiCredentials.DidNotReceive().GetAsync(
            Arg.Any<Guid>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkUsed_ShouldStampTheWorkspaceConnection_WhenItWon()
    {
        UnitOfWork.WorkspaceAiCredentials
            .GetForWorkspace(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns([WorkspaceCredential]);

        var resolver = CreateResolver(new AiDevelopmentKeys());
        var credentials = await resolver.Resolve(UserId, WorkspaceId, TestContext.Current.CancellationToken);

        await resolver.MarkUsed(credentials.Single(), TestContext.Current.CancellationToken);

        WorkspaceCredential.LastUsedAt.Should().NotBeNull();
        UserCredential.LastUsedAt.Should().BeNull();
    }

    private AiCredentialResolver CreateResolver(AiDevelopmentKeys developmentKeys)
    {
        var options = Options.Create(new AiOptions { DevelopmentKeys = developmentKeys });

        return new AiCredentialResolver(UnitOfWork, Protector, options);
    }
}
