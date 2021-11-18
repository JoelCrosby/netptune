using System;

using Hangfire;
using Hangfire.Redis;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Constants;
using Netptune.Core.Jobs;

namespace Netptune.JobClient;

public static class ServiceCollectionExtensions
{
    public static void AddNetptuneJobClient(this IServiceCollection services, Action<JobClientOptions> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        var clientOptions = new JobClientOptions();
        action(clientOptions);

        services.AddHangfire(options =>
        {
            options.UseSimpleAssemblyNameTypeSerializer();
            options.UseRecommendedSerializerSettings();
        });

        GlobalConfiguration.Configuration.UseRedisStorage(clientOptions.ConnectionString,
            new RedisStorageOptions
            {
                Prefix = NetptuneJobConstants.RedisPrefix,
            });

        services.Configure(action);
        services.AddTransient<IJobClient, JobClientService>();
    }
}