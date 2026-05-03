using Microsoft.AspNetCore.DataProtection;

using StackExchange.Redis;

namespace Netptune.App.Configuration;

public static class DataProtectionConfiguration
{
    private const string ApplicationName = "netptune";
    private const string RedisKey = "netptune:data-protection-keys";

    public static IHostApplicationBuilder AddNetptuneDataProtection(
        this IHostApplicationBuilder builder,
        string redisConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        var redisConnection = new Lazy<IConnectionMultiplexer>(
            () => ConnectionMultiplexer.Connect(redisConnectionString));

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => redisConnection.Value);
        builder.Services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToStackExchangeRedis(
                () => redisConnection.Value.GetDatabase(),
                RedisKey);

        return builder;
    }
}
