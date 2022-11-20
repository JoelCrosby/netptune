using System;
using System.Linq;

using Microsoft.Extensions.Configuration;

using Netptune.Core.Utilities;

namespace Netptune.Core.Extensions;

public static class ConfigurationExtensions
{
    public static string GetNetptuneConnectionString(this IConfiguration configuration, string database)
    {
        var appSettingsConString = configuration.GetConnectionString(database);
        var envVar = Environment.GetEnvironmentVariable("DATABASE_URL");

        var connectionString = envVar ?? appSettingsConString;

        if (connectionString is null)
        {
            throw new Exception("An environment variable with the key of 'DATABASE_URL' not found.");
        }

        return ConnectionStringParser.ParseConnectionString(connectionString, database);
    }

    public static string GetNetptuneRedisConnectionString(this IConfiguration configuration)
    {
        var appSettingsConString = configuration.GetConnectionString("redis");
        var envVar = Environment.GetEnvironmentVariable("REDIS_URL");

        var connectionString = envVar ?? appSettingsConString;

        if (connectionString is null)
        {
            throw new Exception("An environment variable with the key of 'REDIS_URL' not found.");
        }

        return ConnectionStringParser.ParseRedis(connectionString);
    }

    public static string GetEnvironmentVariable(this IConfiguration configuration, string key)
    {
        var result = Environment.GetEnvironmentVariable(key);

        if (result is null || string.IsNullOrEmpty(result))
        {
            throw new Exception($"Value for environment variable '{key}' was null or empty.");
        }

        return result;
    }

    public static string[] GetCorsOrigins(this IConfiguration configuration)
    {
        return configuration.GetRequiredSection("CorsOrigins")
            .AsEnumerable()
            .Where(pair => pair.Value is { })
            .Select(pair => pair.Value)
            .ToArray()!;
    }
}
