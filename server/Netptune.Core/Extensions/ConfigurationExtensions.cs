using System.Globalization;

using Microsoft.Extensions.Configuration;

using Netptune.Core.Utilities;

namespace Netptune.Core.Extensions;

public static class ConfigurationExtensions
{
    public static TimeSpan ReadTimeSpan(this IConfiguration configuration, string key, TimeSpan fallback)
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

        var configurationKey = configuration is IConfigurationSection section
            ? $"{section.Path}:{key}"
            : key;

        throw new InvalidOperationException($"{configurationKey} must be a valid TimeSpan value, for example 00:05:00.");
    }

    public static string GetNetptuneConnectionString(this IConfiguration configuration, string database)
    {
        var connectionString = configuration.GetConnectionString(database);

        if (connectionString is null)
        {
            throw new InvalidOperationException($"ConnectionStrings:{database} is not set.");
        }

        return ConnectionStringParser.ParseConnectionString(connectionString, database);
    }

    public static string GetNetptuneRedisConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("redis");

        if (connectionString is null)
        {
            throw new InvalidOperationException("ConnectionStrings:redis is not set.");
        }

        return ConnectionStringParser.ParseRedis(connectionString);
    }

    public static string GetNetptuneNatsConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("nats");

        if (connectionString is null)
        {
            throw new InvalidOperationException("ConnectionStrings:nats is not set.");
        }

        return connectionString;
    }

    public static string[] GetCorsOrigins(this IConfiguration configuration)
    {
        return configuration.GetRequiredSection("CorsOrigins")
            .AsEnumerable()
            .Where(pair => pair.Value is not null)
            .Select(pair => pair.Value)
            .ToArray()!;
    }
}
