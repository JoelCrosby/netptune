using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Security.AntiSSRF;

namespace Netptune.Core.Http;

public static class SafeHttpClientExtensions
{
    public const string ClientName = "netptune-safe-egress";

    public static IServiceCollection AddSafeHttpClient(
        this IServiceCollection services,
        Action<SafeHttpClientOptions>? configure = null)
    {
        services.AddSafeEgressCore(configure);

        services.AddHttpClient(ClientName).AddSafeEgress();

        return services;
    }

    public static IHttpClientBuilder AddSafeHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        Action<SafeHttpClientOptions>? configure = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddSafeEgressCore(configure);

        return services.AddHttpClient<TClient, TImplementation>().AddSafeEgress();
    }

    public static IHttpClientBuilder AddSafeEgress(this IHttpClientBuilder builder)
    {
        builder.Services.AddSafeEgressCore(configure: null);

        return builder
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<SafeHttpClientOptions>>().Value;

                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var options = provider.GetRequiredService<IOptions<SafeHttpClientOptions>>().Value;

                var handler = new AntiSSRFPolicy(PolicyConfigOptions.ExternalOnlyLatest).GetHandler();

                handler.AllowAutoRedirect = true;
                handler.MaxAutomaticRedirections = options.MaxRedirects;
                handler.ConnectTimeout = options.ConnectTimeout;

                return handler;
            })
            .AddHttpMessageHandler<SafeEgressHandler>();
    }

    private static IServiceCollection AddSafeEgressCore(this IServiceCollection services, Action<SafeHttpClientOptions>? configure)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddOptions<SafeHttpClientOptions>();
        services.TryAddTransient<SafeEgressHandler>();

        return services;
    }
}
