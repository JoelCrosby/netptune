using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Execution;
using Netptune.Ai.Providers;
using Netptune.Ai.Tools;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Configuration;

public static class NetptuneAiConfiguration
{
    public static IServiceCollection AddNetptuneAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        services.AddSingleton<IAiChatProvider, AnthropicChatProvider>();
        services.AddSingleton<IAiChatProvider, OpenAiChatProvider>();
        services.AddSingleton<IAiChatProviderFactory, AiChatProviderFactory>();

        services.AddScoped<IAiTool, ListProjectsTool>();
        services.AddScoped<IAiTool, SearchTasksTool>();
        services.AddScoped<IAiTool, ListStatusesTool>();
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();

        services.AddScoped<IAiConversationRunner, AiConversationRunner>();

        return services;
    }
}
