using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Netptune.Activity.Services;

using ActivityMergeOptions = Netptune.Core.Models.Activity.ActivityMergeOptions;

namespace Netptune.Activity;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneActivity(this IServiceCollection services)
    {
        services.TryAddSingleton(Options.Create(BuildMergeOptions(_ => { })));

        services.AddTransient<AuditRetentionJob>();

        services.AddHostedService<ActivityMergeWindowJob>();

        return services;
    }

    public static IServiceCollection AddNetptuneActivity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddNetptuneActivityMerge(merge => configuration.GetSection(ActivityMergeOptions.SectionName).Bind(merge))
            .AddNetptuneActivity();
    }

    public static IServiceCollection AddNetptuneActivityMerge(
        this IServiceCollection services,
        Action<ActivityMergeOptions> configure)
    {
        services.AddSingleton(Options.Create(BuildMergeOptions(configure)));

        return services;
    }

    private static ActivityMergeOptions BuildMergeOptions(Action<ActivityMergeOptions> configure)
    {
        var options = new ActivityMergeOptions();

        configure(options);
        options.Validate();

        return options;
    }
}
