using FluentAssertions;

using Netptune.SourceControl.Configuration;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public class SourceControlArchitectureTests
{
    private const string ProvidersNamespace = "Netptune.SourceControl.Providers";

    // Keeping provider types internal is what makes "no GitHub calls outside the GitHub
    // implementation" a compile error rather than a convention.
    [Fact]
    public void ProviderImplementations_ShouldNotBeVisibleOutsideTheModule()
    {
        var providerTypes = typeof(NetptuneSourceControlConfiguration).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith(ProvidersNamespace, StringComparison.Ordinal) == true)
            .ToList();

        var visibleTypes = providerTypes
            .Where(type => type.IsPublic || type.IsNestedPublic)
            .Select(type => type.FullName)
            .ToList();

        providerTypes.Should().NotBeEmpty();
        visibleTypes.Should().BeEmpty();
    }

    [Fact]
    public void CoreContracts_ShouldNotMentionAnyProviderByName()
    {
        var providerNames = new[] { "GitHub", "AzureDevOps", "GitLab" };

        var coreTypes = typeof(global::Netptune.Core.SourceControl.ISourceControlProvider).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith("Netptune.Core.SourceControl", StringComparison.Ordinal) == true)
            .Select(type => type.Name)
            .ToList();

        var providerSpecific = coreTypes
            .Where(name => providerNames.Any(provider => name.Contains(provider, StringComparison.Ordinal)))
            .ToList();

        coreTypes.Should().NotBeEmpty();
        providerSpecific.Should().BeEmpty();
    }
}
