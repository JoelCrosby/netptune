using FluentAssertions;

using Netptune.Automation.Models;
using Netptune.Automation.Persistence.Actions;
using Netptune.Core.Enums;

using Xunit;

namespace Netptune.Automation.Tests;

public sealed class AutomationActionExecutionHandlerRegistryTests
{
    [Fact]
    public void Constructor_throws_when_an_action_type_has_no_handler()
    {
        var missingType = AutomationActionType.DeleteTask;
        var handlers = Enum.GetValues<AutomationActionType>()
            .Where(type => type != missingType)
            .Select(type => new StubActionExecutionHandler(type));

        var constructRegistry = () => new AutomationActionExecutionHandlerRegistry(handlers);

        constructRegistry.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*{missingType}*");
    }

    [Fact]
    public void Constructor_throws_when_an_action_type_has_multiple_handlers()
    {
        var duplicateType = AutomationActionType.FlagTask;
        var handlers = Enum.GetValues<AutomationActionType>()
            .Select<AutomationActionType, IAutomationActionExecutionHandler>(
                type => new StubActionExecutionHandler(type))
            .Append(new StubActionExecutionHandler(duplicateType));

        var constructRegistry = () => new AutomationActionExecutionHandlerRegistry(handlers);

        constructRegistry.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*{duplicateType}*");
    }

    private sealed class StubActionExecutionHandler : IAutomationActionExecutionHandler
    {
        public StubActionExecutionHandler(AutomationActionType type)
        {
            Type = type;
        }

        public AutomationActionType Type { get; }

        public Task<AutomationActionExecutionOutcome> Execute(
            PlannedAutomationAction action,
            AutomationPersistenceState state,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
