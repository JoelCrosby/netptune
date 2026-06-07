using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Execution;
using Netptune.Automation.Matching;
using Netptune.Automation.Notifications;
using Netptune.Automation.Persistence;
using Netptune.Automation.Planning;
using Netptune.Automation.Scheduling;

namespace Netptune.Automation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneAutomation(this IServiceCollection services)
    {
        return services.AddNetptuneAutomation(_ => { });
    }

    public static IServiceCollection AddNetptuneAutomation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddNetptuneAutomation(options =>
        {
            var section = configuration.GetSection(ScheduleOptions.SectionName);

            options.StartupDelay = ReadTimeSpan(section, nameof(ScheduleOptions.StartupDelay), options.StartupDelay);
            options.RunInterval = ReadTimeSpan(section, nameof(ScheduleOptions.RunInterval), options.RunInterval);
        });
    }

    public static IServiceCollection AddNetptuneAutomation(
        this IServiceCollection services,
        Action<ScheduleOptions> configure)
    {
        var options = new ScheduleOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(Options.Create(options));
        services.AddScoped<StatusChangedAutomationRuleMatcher>();
        services.AddScoped<UnassignedTaskAutomationRuleMatcher>();
        services.AddScoped<RuleExecutor>();
        services.AddScoped<ActionPlanner>();
        services.AddScoped<FlagPlanner>();
        services.AddScoped<RunPersistenceService>();
        services.AddScoped<NotificationPublisher>();
        services.AddScoped<ExecutionService>();
        services.AddScoped<IExecutionService>(provider => provider.GetRequiredService<ExecutionService>());
        services.AddHostedService<ScheduleService>();

        return services;
    }

    private static TimeSpan ReadTimeSpan(IConfiguration configuration, string key, TimeSpan fallback)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"{ScheduleOptions.SectionName}:{key} must be a valid TimeSpan value, for example 00:05:00.");
    }
}
