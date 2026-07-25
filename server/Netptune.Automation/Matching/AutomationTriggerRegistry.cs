using Netptune.Automation.Common;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Automation.Matching;

internal sealed class AutomationTriggerRegistry
{
    private readonly List<IAutomationRuleMatcher> Matchers;
    private readonly Dictionary<AutomationTriggerType, IScheduledRuleMatcher> ScheduledMatchers;

    public IReadOnlyList<AutomationTriggerType> ScheduledTriggerTypes { get; }

    public AutomationTriggerRegistry(IEnumerable<IAutomationRuleMatcher> matchers)
    {
        Matchers = matchers.ToList();

        var duplicateTypes = Matchers
            .GroupBy(matcher => matcher.TriggerType)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Multiple automation rule matchers are registered for: {string.Join(", ", duplicateTypes)}.");
        }

        var registeredTypes = Matchers.Select(matcher => matcher.TriggerType).ToHashSet();
        var missingTypes = Enum.GetValues<AutomationTriggerType>()
            .Where(type => !registeredTypes.Contains(type))
            .ToList();

        if (missingTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"No automation rule matcher is registered for: {string.Join(", ", missingTypes)}.");
        }

        var scheduledMatchers = Matchers.OfType<IScheduledRuleMatcher>().ToList();

        ScheduledMatchers = scheduledMatchers.ToDictionary(matcher => matcher.TriggerType);
        ScheduledTriggerTypes = scheduledMatchers.Select(matcher => matcher.TriggerType).ToList();
    }

    public IScheduledRuleMatcher GetScheduledMatcher(AutomationTriggerType triggerType)
    {
        var matcher = ScheduledMatchers.GetValueOrDefault(triggerType);

        if (matcher is null)
        {
            throw new InvalidOperationException(
                $"No scheduled automation rule matcher is registered for '{triggerType}'.");
        }

        return matcher;
    }

    public List<IEventRuleMatcher<TMessage>> GetEventMatchers<TMessage>() where TMessage : IEventMessage
    {
        var matchers = Matchers.OfType<IEventRuleMatcher<TMessage>>().ToList();

        if (matchers.Count == 0)
        {
            throw new InvalidOperationException(
                $"No automation rule matcher is registered for '{typeof(TMessage).Name}'.");
        }

        return matchers;
    }
}
