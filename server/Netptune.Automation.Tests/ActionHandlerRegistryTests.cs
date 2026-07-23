using FluentAssertions;

using Netptune.Automation.Models;
using Netptune.Automation.Persistence.Actions;
using Netptune.Core.Enums;

using Xunit;

namespace Netptune.Automation.Tests;

public sealed class ActionHandlerRegistryTests
{
    [Fact]
    public void Constructor_throws_when_an_action_type_has_no_handler()
    {
        var missingType = AutomationActionType.DeleteTask;
        var handlers = Enum.GetValues<AutomationActionType>()
            .Where(type => type != missingType)
            .Select(type => new StubHandler(type));

        var constructRegistry = () => new ActionHandlerRegistry(handlers);

        constructRegistry.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*{missingType}*");
    }

    [Fact]
    public void Constructor_throws_when_an_action_type_has_multiple_handlers()
    {
        var duplicateType = AutomationActionType.FlagTask;
        var handlers = Enum.GetValues<AutomationActionType>()
            .Select<AutomationActionType, IActionExecutionHandler>(
                type => new StubHandler(type))
            .Append(new StubHandler(duplicateType));

        var constructRegistry = () => new ActionHandlerRegistry(handlers);

        constructRegistry.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*{duplicateType}*");
    }

    private sealed class StubHandler : IActionExecutionHandler
    {
        public StubHandler(AutomationActionType type)
        {
            Type = type;
        }

        public AutomationActionType Type { get; }

        public Task<ActionOutcome> Execute(
            PlannedAutomationAction action,
            AutomationPersistenceState state,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
