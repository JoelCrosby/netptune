using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Netptune.ServiceDefaults.Networking;

public static class ClientIpAddressExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddNetptuneClientIpAddress(IConfiguration configuration)
        {
            services.Configure<TrustedProxyOptions>(configuration.GetSection(TrustedProxyOptions.SectionName));
            services.AddSingleton<IValidateOptions<TrustedProxyOptions>, TrustedProxyOptionsValidator>();
            services.AddOptions<TrustedProxyOptions>().ValidateOnStart();
            services.AddSingleton<TrustedProxyRegistry>();

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseNetptuneClientIpAddress()
        {
            return app.UseMiddleware<ClientIpAddressMiddleware>();
        }
    }

    extension(HttpContext context)
    {
        // Null only when the peer address itself is unavailable, which in practice means an in-process
        // test server rather than a real client.
        public string? GetClientIpAddress()
        {
            return context.Items.TryGetValue(ClientIpAddressMiddleware.ItemKey, out var address)
                ? address as string
                : context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
