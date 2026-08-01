using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiModelCatalogTests
{
    [Fact]
    public void Catalog_ShouldExposeExactlyOneDefaultPerProvider()
    {
        foreach (var provider in Enum.GetValues<AiProvider>())
        {
            var defaults = AiModels.Catalog
                .Where(model => model.Provider == provider && model.IsDefault)
                .ToList();

            defaults.Should().ContainSingle($"{provider} needs one default model");
        }
    }

    [Fact]
    public void Catalog_ShouldNotRepeatModelIdentifiers()
    {
        var identifiers = AiModels.Catalog.Select(model => model.Id).ToList();

        identifiers.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void IsSupported_ShouldRejectAModelFromAnotherProvider()
    {
        AiModels.IsSupported(AiProvider.Anthropic, AiModels.AnthropicDefault).Should().BeTrue();
        AiModels.IsSupported(AiProvider.OpenAi, AiModels.AnthropicDefault).Should().BeFalse();
    }

    [Fact]
    public void IsSupported_ShouldRejectAnEmptyModel()
    {
        AiModels.IsSupported(AiProvider.Anthropic, null).Should().BeFalse();
        AiModels.IsSupported(AiProvider.Anthropic, "   ").Should().BeFalse();
        AiModels.IsSupported(AiProvider.Anthropic, "gpt-9000").Should().BeFalse();
    }

    [Fact]
    public void ProviderFor_ShouldResolveTheOwningProvider()
    {
        AiModels.ProviderFor(AiModels.OpenAiDefault).Should().Be(AiProvider.OpenAi);
        AiModels.ProviderFor(AiModels.AnthropicDefault).Should().Be(AiProvider.Anthropic);
        AiModels.ProviderFor("gpt-9000").Should().BeNull();
    }

    [Fact]
    public void Defaults_ShouldMatchTheCatalogDefaults()
    {
        var anthropic = AiModels.Catalog.Single(model => model.Provider == AiProvider.Anthropic && model.IsDefault);
        var openAi = AiModels.Catalog.Single(model => model.Provider == AiProvider.OpenAi && model.IsDefault);

        anthropic.Id.Should().Be(AiModels.AnthropicDefault);
        openAi.Id.Should().Be(AiModels.OpenAiDefault);
    }
}
