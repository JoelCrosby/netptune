using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.Core.Services.Automations;

namespace Netptune.Automation.Actions;

public static class AutomationActionServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneAutomationActions(this IServiceCollection services)
    {
        var notifyAction = ServiceDescriptor.Singleton<IAutomationAction, NotifyTaskAssigneesAutomationAction>();
        var flagAction = ServiceDescriptor.Singleton<IAutomationAction, FlagTaskAutomationAction>();
        var updateAction = ServiceDescriptor.Singleton<IAutomationAction, UpdateTaskAutomationAction>();
        var commentAction = ServiceDescriptor.Singleton<IAutomationAction, AddCommentAutomationAction>();

        services.TryAddEnumerable(notifyAction);
        services.TryAddEnumerable(flagAction);
        services.TryAddEnumerable(updateAction);
        services.TryAddEnumerable(commentAction);
        services.TryAddSingleton<IAutomationActionRegistry, AutomationActionRegistry>();

        return services;
    }
}
