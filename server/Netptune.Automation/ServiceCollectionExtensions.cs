using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Actions;
using Netptune.Automation.Execution;
using Netptune.Automation.Execution.Actions;
using Netptune.Automation.Matching;
using Netptune.Automation.Notifications;
using Netptune.Automation.Persistence;
using Netptune.Automation.Persistence.Actions;
using Netptune.Automation.Planning;
using Netptune.Automation.Scheduling;
using Netptune.Core.Extensions;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Services.ProjectTasks;

namespace Netptune.Automation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneAutomation(this IServiceCollection services)
    {
        return services.AddNetptuneAutomation(_ => { });
    }

    public static IServiceCollection AddNetptuneAutomation(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddNetptuneAutomation(options =>
        {
            var section = configuration.GetSection(ScheduleOptions.SectionName);

            options.StartupDelay = section.ReadTimeSpan(nameof(ScheduleOptions.StartupDelay), options.StartupDelay);
            options.RunInterval = section.ReadTimeSpan(nameof(ScheduleOptions.RunInterval), options.RunInterval);
            options.DelayedActionRunInterval = section.ReadTimeSpan(nameof(ScheduleOptions.DelayedActionRunInterval), options.DelayedActionRunInterval);
            options.DelayedActionClaimLease = section.ReadTimeSpan(nameof(ScheduleOptions.DelayedActionClaimLease), options.DelayedActionClaimLease);
            options.DelayedActionRetryDelay = section.ReadTimeSpan(nameof(ScheduleOptions.DelayedActionRetryDelay), options.DelayedActionRetryDelay);
            options.DelayedActionMaxAttempts = section.GetValue(nameof(ScheduleOptions.DelayedActionMaxAttempts), options.DelayedActionMaxAttempts);
            options.DelayedActionBatchSize = section.GetValue(nameof(ScheduleOptions.DelayedActionBatchSize), options.DelayedActionBatchSize);
        });
    }

    public static IServiceCollection AddNetptuneAutomation(this IServiceCollection services, Action<ScheduleOptions> configure)
    {
        var options = new ScheduleOptions();
        configure(options);
        options.Validate();

        services.AddNetptuneAutomationActions();
        services.TryAddScoped<ITaskMutationPipeline, TaskMutationPipeline>();

        services.AddSingleton(Options.Create(options));
        services.AddScoped<TaskChangedRuleMatcher>();
        services.AddScoped<UnassignedTaskRuleMatcher>();
        services.AddScoped<DueDateRuleMatcher>();
        services.AddScoped<RuleExecutor>();
        services.AddScoped<ActionPlanner>();
        services.AddScoped<FlagPlanner>();
        services.AddScoped<ActionHandlerRegistry>();
        services.AddScoped<RunPersistenceService>();
        services.AddScoped<NotificationPublisher>();
        services.AddScoped<ExecutionService>();
        services.AddScoped<ScheduledActionService>();
        services.AddScoped<ScheduledActionEligibilityEvaluator>();
        services.AddScoped<IScheduledActionHandler, ScheduledDeleteTaskHandler>();
        services.AddScoped<ScheduledActionHandlerRegistry>();

        services.AddScoped<IExecutionService>(provider => provider.GetRequiredService<ExecutionService>());

        services.AddScoped<IActionExecutionHandler, NotifyTaskAssigneesHandler>();
        services.AddScoped<IActionExecutionHandler, FlagTaskHandler>();
        services.AddScoped<IActionExecutionHandler, UpdateTaskHandler>();
        services.AddScoped<IActionExecutionHandler, AddCommentHandler>();
        services.AddScoped<IActionExecutionHandler, DeleteTaskHandler>();

        services.AddHostedService<ScheduleService>();
        services.AddHostedService<DelayedActionScheduleService>();

        return services;
    }
}
