using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Automation.Actions;
using Netptune.Core.Enums;
using Netptune.Core.Services.Automations;

using Xunit;

namespace Netptune.UnitTests.Netptune.Automation.Actions;

public sealed class AutomationActionRegistryTests
{
    [Fact]
    public void AddNetptuneAutomationActions_ShouldRegisterEveryActionType()
    {
        var services = new ServiceCollection();
        services.AddNetptuneAutomationActions();
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAutomationActionRegistry>();

        var registeredActions = Enum.GetValues<AutomationActionType>()
            .Select(registry.Find)
            .ToList();

        registeredActions.Should().NotContain(action => action == null);
        registeredActions.Select(action => action!.Type).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Constructor_ShouldFail_WhenAnActionTypeIsMissing()
    {
        var createRegistry = () => new AutomationActionRegistry([]);

        createRegistry.Should().Throw<InvalidOperationException>()
            .WithMessage("*No automation action is registered*");
    }
}
