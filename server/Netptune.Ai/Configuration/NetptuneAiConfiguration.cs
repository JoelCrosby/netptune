using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
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
        services.AddScoped<IAiTool, ListMembersTool>();
        services.AddScoped<IAiTool, CreateTaskTool>();
        services.AddScoped<IAiTool, UpdateTaskTool>();
        services.AddScoped<IAiTool, AssignTaskTool>();
        services.AddScoped<IAiTool, ListSprintsTool>();
        services.AddScoped<IAiTool, MoveTaskToSprintTool>();
        services.AddScoped<IAiTool, ListTagsTool>();
        services.AddScoped<IAiTool, SetTaskTagsTool>();
        services.AddScoped<IAiTool, AddTaskCommentTool>();

        services.AddScoped<IAiChangeHandler, CreateTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, AssignTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, MoveTaskToSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, SetTaskTagsChangeHandler>();
        services.AddScoped<IAiChangeHandler, AddTaskCommentChangeHandler>();
        services.AddScoped<IAiChangeSetBuilder, AiChangeSetBuilder>();
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();

        services.AddScoped<IAiConversationRunner, AiConversationRunner>();
        services.AddScoped<IAiSystemPromptBuilder, AiSystemPromptBuilder>();
        services.AddScoped<IAiConversationService, AiConversationService>();
        services.AddScoped<IAiChangeSetApplier, AiChangeSetApplier>();

        return services;
    }
}
