using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.ViewModels.Ai;
using Netptune.Handlers.Ai.Commands;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai.Reviews;

public class AiReviewModelTests
{
    [Theory]
    [InlineData(AiProvider.Anthropic, null, AiModels.AnthropicReviewModel)]
    [InlineData(AiProvider.Anthropic, "claude-opus-5", "claude-opus-5")]
    [InlineData(AiProvider.Anthropic, "claude-retired-1", AiModels.AnthropicReviewModel)]
    [InlineData(AiProvider.Anthropic, "gpt-5.6-terra", AiModels.AnthropicReviewModel)]
    [InlineData(AiProvider.OpenAi, null, AiModels.OpenAiReviewModel)]
    public void ResolveReviewModel_ShouldFallBackToTheDefault_UnlessTheSavedModelIsSupported(
        AiProvider provider,
        string? savedModel,
        string expected)
    {
        AiModels.ResolveReviewModel(provider, savedModel).Should().Be(expected);
    }

    [Fact]
    public void Resolve_ShouldCarryTheReviewModel_FromWhicheverConnectionWins()
    {
        var workspaceCredential = new WorkspaceAiCredential
        {
            Id = Guid.NewGuid(),
            Provider = AiProvider.Anthropic,
            Label = "Workspace",
            Secret = [],
            SecretHint = "1234",
            ReviewModel = "claude-haiku-4-5",
        };

        var userCredential = new UserAiCredential
        {
            Id = Guid.NewGuid(),
            UserId = "user",
            Provider = AiProvider.Anthropic,
            Label = "Mine",
            Secret = [],
            SecretHint = "5678",
            ReviewModel = "claude-opus-5",
        };

        var workspaceOnly = AiCredentialResolution.Resolve([], [workspaceCredential]).Single();
        var both = AiCredentialResolution.Resolve([userCredential], [workspaceCredential]).Single();

        workspaceOnly.ReviewModel.Should().Be("claude-haiku-4-5");
        both.ReviewModel.Should().Be("claude-opus-5");
    }

    [Fact]
    public void ValidateModels_ShouldRejectAModelFromAnotherProvider_AndTreatBlankAsDefault()
    {
        var blank = SaveAiCredentialValidation.ValidateModels(AiProvider.Anthropic, " ", "");
        var wrongProvider = SaveAiCredentialValidation.ValidateModels(AiProvider.Anthropic, null, "gpt-5.6-terra");

        blank.Should().Be(new AiCredentialModels(null, null));
        wrongProvider.Should().BeNull();
    }

    [Fact]
    public void ValidateModelUpdate_ShouldKeepARetiredModel_ThatIsLeftUnchanged()
    {
        var stored = new AiCredentialModels("claude-retired-1", null);
        var keepsRetired = new AiCredentialModels("claude-retired-1", "claude-haiku-4-5");
        var picksRetired = new AiCredentialModels("claude-opus-5", "claude-retired-1");

        var kept = SaveAiCredentialValidation.ValidateModelUpdate(AiProvider.Anthropic, stored, keepsRetired);
        var picked = SaveAiCredentialValidation.ValidateModelUpdate(AiProvider.Anthropic, stored, picksRetired);

        kept.Should().Be(keepsRetired);
        picked.Should().BeNull();
    }

    [Theory]
    [InlineData(true, 0, AiReviewReadiness.Stale)]
    [InlineData(false, 2, AiReviewReadiness.NeedsWork)]
    [InlineData(false, 0, AiReviewReadiness.Ready)]
    public void ReadinessFor_ShouldPutStaleFirst_ThenOpenFindings(
        bool isStale,
        int openCount,
        AiReviewReadiness expected)
    {
        AiReviewReadinessRules.For(isStale, openCount).Should().Be(expected);
    }
}
