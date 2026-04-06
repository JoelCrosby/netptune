using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Netptune.Cache.Redis;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;

namespace Netptune.Cache;

public static class ServiceCollectionExtensions
{
    private static void AddNetptuneCache(this IServiceCollection services, Action<RedisCacheOptions> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        var options = new RedisCacheOptions();
        action(options);

        services.Configure(action);

        services.TryAddSingleton<ICacheProvider, RedisCache>();

        services.AddScoped<IUserCache, UserCache>();
        services.AddScoped<IWorkspaceUserCache, WorkspaceUserCache>();
        services.AddScoped<IInviteCache, InviteCache>();
        services.AddScoped<IWorkspaceCache, WorkspaceCache>();
    }

    public static IHostApplicationBuilder AddNetptuneCache(this IHostApplicationBuilder builder, Action<RedisCacheOptions> action)
    {
        builder.Services.AddNetptuneCache(action);

        return builder;
    }
}
