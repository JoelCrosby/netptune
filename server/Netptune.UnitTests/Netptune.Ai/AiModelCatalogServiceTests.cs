using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Ai.Execution;
using Netptune.Ai.Providers;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiModelCatalogServiceTests
{
    private const string AnthropicKey = "anthropic-key";

    private readonly IAiModelEffortSource Source = Substitute.For<IAiModelEffortSource>();
    private readonly IAiCredentialProtector Protector = Substitute.For<IAiCredentialProtector>();
    private readonly AiModelCatalog Catalog;

    private readonly AiResolvedCredential AnthropicCredential = new()
    {
        Provider = AiProvider.Anthropic,
        Secret = [1],
    };

    private readonly AiResolvedCredential OpenAiCredential = new()
    {
        Provider = AiProvider.OpenAi,
        Secret = [2],
    };

    public AiModelCatalogServiceTests()
    {
        Source.Provider.Returns(AiProvider.Anthropic);
        Protector.Unprotect(Arg.Any<byte[]>()).Returns(AnthropicKey);

        Catalog = new AiModelCatalog([Source], Protector, NullLogger<AiModelCatalog>.Instance);
    }

    [Fact]
    public async Task EffortsFor_ShouldUseTheLiveLevels_WhenTheProviderReportsTheModel()
    {
        GivenLiveEfforts(new() { [AiModels.AnthropicDefault] = [AiEffort.Low, AiEffort.High] });

        var efforts = await Catalog.EffortsFor(
            AnthropicCredential,
            AiModels.AnthropicDefault,
            TestContext.Current.CancellationToken);

        efforts.Should().Equal(AiEffort.Low, AiEffort.High);
    }

    [Fact]
    public async Task EffortsFor_ShouldUseTheCatalogLevels_WhenTheProviderOmitsTheModel()
    {
        GivenLiveEfforts(new() { ["claude-other"] = [AiEffort.Low] });

        var efforts = await Catalog.EffortsFor(
            AnthropicCredential,
            AiModels.AnthropicDefault,
            TestContext.Current.CancellationToken);

        efforts.Should().Equal(AiModels.EffortsFor(AiModels.AnthropicDefault));
    }

    [Fact]
    public async Task EffortsFor_ShouldUseTheCatalogLevels_WhenTheProviderHasNoSource()
    {
        var efforts = await Catalog.EffortsFor(
            OpenAiCredential,
            AiModels.OpenAiDefault,
            TestContext.Current.CancellationToken);

        efforts.Should().Equal(AiModels.EffortsFor(AiModels.OpenAiDefault));
        await Source.DidNotReceiveWithAnyArgs().GetEfforts(default!, default);
    }

    [Fact]
    public async Task EffortsFor_ShouldReuseTheCachedLevels_WhenCalledAgain()
    {
        GivenLiveEfforts(new() { [AiModels.AnthropicDefault] = [AiEffort.Low] });

        var cancellationToken = TestContext.Current.CancellationToken;

        await Catalog.EffortsFor(AnthropicCredential, AiModels.AnthropicDefault, cancellationToken);
        await Catalog.EffortsFor(AnthropicCredential, AiModels.AnthropicDefault, cancellationToken);

        await Source.Received(1).GetEfforts(AnthropicKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EffortsFor_ShouldUseTheCatalogLevels_WhenTheProviderTimesOut()
    {
        var timeout = new TaskCanceledException("timed out", new TimeoutException());

        Source.GetEfforts(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(timeout);

        var efforts = await Catalog.EffortsFor(
            AnthropicCredential,
            AiModels.AnthropicDefault,
            TestContext.Current.CancellationToken);

        efforts.Should().Equal(AiModels.EffortsFor(AiModels.AnthropicDefault));
    }

    [Fact]
    public async Task EffortsFor_ShouldNotRetryImmediately_WhenTheProviderFails()
    {
        Source.GetEfforts(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("unreachable"));

        var cancellationToken = TestContext.Current.CancellationToken;

        await Catalog.EffortsFor(AnthropicCredential, AiModels.AnthropicDefault, cancellationToken);
        await Catalog.EffortsFor(AnthropicCredential, AiModels.AnthropicDefault, cancellationToken);

        await Source.Received(1).GetEfforts(AnthropicKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EffortsFor_ShouldPropagateCancellation_WhenTheCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        Source.GetEfforts(Arg.Any<string>(), cancellation.Token)
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var act = () => Catalog.EffortsFor(AnthropicCredential, AiModels.AnthropicDefault, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task List_ShouldApplyLiveLevelsOnlyToTheReportingProvider()
    {
        GivenLiveEfforts(new() { [AiModels.AnthropicDefault] = [AiEffort.Medium] });

        var models = await Catalog.List(
            [AnthropicCredential, OpenAiCredential],
            TestContext.Current.CancellationToken);

        var anthropic = models.Single(model => model.Id == AiModels.AnthropicDefault);
        var openAi = models.Single(model => model.Id == AiModels.OpenAiDefault);

        anthropic.Efforts.Should().Equal(AiEffort.Medium);
        openAi.Efforts.Should().Equal(AiModels.EffortsFor(AiModels.OpenAiDefault));
        models.Should().HaveCount(AiModels.Catalog.Count);
    }

    [Fact]
    public async Task List_ShouldUseTheCatalogLevels_WhenThereAreNoCredentials()
    {
        var models = await Catalog.List([], TestContext.Current.CancellationToken);

        models.Should().BeEquivalentTo(AiModels.Catalog);
        await Source.DidNotReceiveWithAnyArgs().GetEfforts(default!, default);
    }

    private void GivenLiveEfforts(Dictionary<string, IReadOnlyList<AiEffort>> efforts)
    {
        Source.GetEfforts(AnthropicKey, Arg.Any<CancellationToken>()).Returns(efforts);
    }
}
