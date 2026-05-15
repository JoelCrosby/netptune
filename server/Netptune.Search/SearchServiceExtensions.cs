using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Core.Services.Search;

namespace Netptune.Search;

public static class SearchServiceExtensions
{
    public static IHostApplicationBuilder AddNetptuneSearch(this IHostApplicationBuilder builder)
    {
        builder.AddMeilisearchClient("meilisearch");
        builder.Services.AddSingleton<IMeilisearchService, MeilisearchService>();

        return builder;
    }
}
