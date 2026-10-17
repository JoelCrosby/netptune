using FluentAssertions;

using Netptune.Core.SourceControl;
using Netptune.SourceControl.Registry;
using Netptune.UnitTests.Netptune.SourceControl.Support;

using Xunit;

namespace Netptune.UnitTests.Netptune.SourceControl;

public sealed class SourceControlProviderRegistryTests : IDisposable
{
    private readonly GitHubTestHarness GitHub = new();

    [Fact]
    public async Task Registry_ShouldServeAStubProviderAlongsideGitHub()
    {
        var fake = new FakeSourceControlProvider(SourceControlProvider.GitLab);
        var registry = new SourceControlProviderRegistry([GitHub.Provider, fake]);
        var connection = GitHubTestHarness.CreateConnection() with { Provider = SourceControlProvider.GitLab };

        var context = registry.CreateContext(connection);
        var provider = registry.Resolve(SourceControlProvider.GitLab);
        var cancellationToken = TestContext.Current.CancellationToken;
        var listing = provider.Client.ListRepositories(context, cancellationToken);
        var repositories = await listing.ToListAsync(cancellationToken);

        registry.All.Select(registered => registered.Provider).Should().Equal(
            SourceControlProvider.GitHub,
            SourceControlProvider.GitLab);
        registry.Resolve(SourceControlProvider.GitHub).Should().BeSameAs(GitHub.Provider);
        repositories.Should().ContainSingle().Which.FullName.Should().Be("fake-group/fake-repo");
        fake.FakeConnector.TokenRequests.Should().Be(1);
    }

    [Fact]
    public void Constructor_ShouldRejectTwoProvidersOfTheSameKind()
    {
        FakeSourceControlProvider[] duplicates = [new(), new()];
        var create = () => new SourceControlProviderRegistry(duplicates);

        create.Should().Throw<InvalidOperationException>().WithMessage("*GitLab*");
    }

    [Fact]
    public void Resolve_ShouldThrowForAProviderThatIsNotEnabled()
    {
        var registry = new SourceControlProviderRegistry([GitHub.Provider]);

        var resolve = () => registry.Resolve(SourceControlProvider.AzureDevOps);

        registry.IsEnabled(SourceControlProvider.AzureDevOps).Should().BeFalse();
        resolve.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetEffectiveCapabilities_ShouldOnlyIncludeWhatTheProviderSupportsAndTheConnectionWasGranted()
    {
        var registry = new SourceControlProviderRegistry([GitHub.Provider]);
        var granted = SourceControlCapabilities.ReadBranches | SourceControlCapabilities.IssueSync;
        var connection = GitHubTestHarness.CreateConnection(granted);

        var effective = registry.GetEffectiveCapabilities(connection);

        effective.Should().Be(SourceControlCapabilities.ReadBranches);
    }

    [Fact]
    public void GetEffectiveCapabilities_ShouldBeNoneForASuspendedConnection()
    {
        var registry = new SourceControlProviderRegistry([GitHub.Provider]);
        var connection = GitHubTestHarness.CreateConnection() with { Status = ScmConnectionStatus.Suspended };

        var effective = registry.GetEffectiveCapabilities(connection);

        effective.Should().Be(SourceControlCapabilities.None);
    }

    [Fact]
    public void Require_ShouldFailWhenTheConnectionWasNotGrantedTheCapability()
    {
        var registry = new SourceControlProviderRegistry([GitHub.Provider]);
        var readOnly = GitHubTestHarness.CreateConnection(SourceControlCapabilities.ReadBranches);

        var createBranch = registry.Require(readOnly, SourceControlCapabilities.CreateBranch);
        var readBranches = registry.Require(readOnly, SourceControlCapabilities.ReadBranches);

        createBranch.IsSuccess.Should().BeFalse();
        createBranch.Message.Should().Contain("CreateBranch");
        readBranches.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Require_ShouldFailForAProviderThatIsNotEnabled()
    {
        var registry = new SourceControlProviderRegistry([]);

        var result = registry.Require(GitHubTestHarness.CreateConnection(), SourceControlCapabilities.ReadBranches);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not enabled");
    }

    [Fact]
    public void CreateContext_ShouldCarryTheEffectiveCapabilities()
    {
        var registry = new SourceControlProviderRegistry([GitHub.Provider]);
        var connection = GitHubTestHarness.CreateConnection(SourceControlCapabilities.ListRepositories);

        var context = registry.CreateContext(connection);

        context.Connection.Should().BeSameAs(connection);
        context.Capabilities.Should().Be(SourceControlCapabilities.ListRepositories);
    }

    public void Dispose()
    {
        GitHub.Dispose();
    }
}
