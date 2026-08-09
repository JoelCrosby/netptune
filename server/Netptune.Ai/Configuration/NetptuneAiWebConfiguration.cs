using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Tools;
using Netptune.Ai.Web;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Configuration;

public static class NetptuneAiWebConfiguration
{
    private const string UserAgent = "NetptuneAssistant/1.0 (+https://netptune.com)";

    public static IServiceCollection AddNetptuneAiWeb(this IServiceCollection services, AiWebOptions options)
    {
        services
            .AddHttpClient<IWebContentFetcher, WebContentFetcher>(WebContentFetcher.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.MaxResponseContentBufferSize = options.MaxResponseBytes;
            })
            .ConfigurePrimaryHttpMessageHandler(CreateGuardedHandler);

        services.AddScoped<IAiTool, WebFetchTool>();
        services.AddScoped<IAiTool, ReadWebDocumentTool>();

        services.AddScoped<IWebSearchEngine, BraveSearchEngine>();
        services.AddScoped<IWebSearchEngine, GoogleSearchEngine>();
        services.AddScoped<IWebSearchEngine, SearxngSearchEngine>();

        services.AddHttpClient<IWebSearchProvider, WorkspaceWebSearchProvider>(
            WorkspaceWebSearchProvider.HttpClientName,
            client =>
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            });

        services.AddScoped<IAiTool, WebSearchTool>();

        return services;
    }

    // The redirect loop checks each hop by name, which a DNS answer that changes between the check and the
    // connect would slip past. This refuses the socket itself, so a rebind cannot reach a private address.
    private static SocketsHttpHandler CreateGuardedHandler()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);

                foreach (var address in addresses)
                {
                    var verdict = WebEgressGuard.CheckAddress(address);

                    if (!verdict.IsAllowed)
                    {
                        throw new HttpRequestException(verdict.Reason);
                    }
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);

                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();

                    throw;
                }
            },
        };
    }
}
