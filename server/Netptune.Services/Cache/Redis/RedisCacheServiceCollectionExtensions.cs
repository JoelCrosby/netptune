using System;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Cache.Common;

namespace Netptune.Services.Cache.Redis;

public static class RedisCacheServiceCollectionExtensions
{
    public static void AddNetptuneRedis(this IServiceCollection services, Action<RedisCacheOptions> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        var options = new RedisCacheOptions();
        action(options);

        services.Configure(action);

        services.AddSingleton<ICacheProvider, RedisCache>();
    }
}