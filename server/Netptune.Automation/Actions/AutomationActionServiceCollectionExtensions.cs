using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Netptune.Core.Services.Automations;

namespace Netptune.Automation.Actions;

public static class AutomationActionServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneAutomationActions(this IServiceCollection services)
    {
        var notifyAction = ServiceDescriptor.Singleton<IAutomationAction, NotifyTaskAssigneesAction>();
        var flagAction = ServiceDescriptor.Singleton<IAutomationAction, FlagTaskAction>();
        var updateAction = ServiceDescriptor.Singleton<IAutomationAction, UpdateTaskAction>();
        var commentAction = ServiceDescriptor.Singleton<IAutomationAction, AddCommentAction>();
        var deleteAction = ServiceDescriptor.Singleton<IAutomationAction, DeleteTaskAction>();
        var createAction = ServiceDescriptor.Singleton<IAutomationAction, CreateTaskAction>();
        var relationAction = ServiceDescriptor.Singleton<IAutomationAction, ManageTaskRelationAction>();

        services.TryAddEnumerable(notifyAction);
        services.TryAddEnumerable(flagAction);
        services.TryAddEnumerable(updateAction);
        services.TryAddEnumerable(commentAction);
        services.TryAddEnumerable(deleteAction);
        services.TryAddEnumerable(createAction);
        services.TryAddEnumerable(relationAction);
        services.TryAddSingleton<IAutomationActionRegistry, AutomationActionRegistry>();

        return services;
    }
}
