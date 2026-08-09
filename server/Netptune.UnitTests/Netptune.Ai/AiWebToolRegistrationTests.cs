using FluentAssertions;

using Mediator;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Configuration;
using Netptune.Ai.Web;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiWebToolRegistrationTests
{
    [Fact]
    public void EveryWebTool_ShouldResolve_WithoutAnyConfiguration()
    {
        using var provider = CreateProvider([]);

        var names = ToolNames(provider);

        names.Should().Contain("web_fetch");
        names.Should().Contain("read_web_document");
        names.Should().Contain(
            "web_search",
            "the search credential is per workspace, so registration cannot depend on configuration");
    }

    [Fact]
    public void EverySearchEngine_ShouldResolve()
    {
        using var provider = CreateProvider([]);

        var providers = provider.GetServices<IWebSearchEngine>().Select(engine => engine.Provider).ToList();

        providers.Should().BeEquivalentTo(
            [WebSearchProvider.Brave, WebSearchProvider.Google, WebSearchProvider.Searxng]);
    }

    [Fact]
    public void EveryWebTool_ShouldRequireTheWebPermission()
    {
        using var provider = CreateProvider([]);

        var webTools = provider
            .GetServices<IAiTool>()
            .Where(tool => tool.Name.Contains("web", StringComparison.Ordinal))
            .ToList();

        webTools.Should().HaveCount(3);
        webTools.Should().OnlyContain(tool => tool.RequiredPermissions.Contains("assistant.use_web"));
    }

    private static List<string> ToolNames(ServiceProvider provider)
    {
        return provider.GetServices<IAiTool>().Select(tool => tool.Name).ToList();
    }

    private static ServiceProvider CreateProvider(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Substitute.For<IMediator>());
        services.AddSingleton(Substitute.For<INetptuneUnitOfWork>());
        services.AddSingleton(Substitute.For<IIdentityService>());
        services.AddSingleton(Substitute.For<IAiCredentialProtector>());
        services.AddSingleton(Substitute.For<IAiExecutionContext>());
        services.AddNetptuneAi(configuration);

        return services.BuildServiceProvider();
    }
}
