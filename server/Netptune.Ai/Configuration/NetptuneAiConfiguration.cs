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
        services.AddScoped<IAiTool, ListBoardsTool>();
        services.AddScoped<IAiTool, ListBoardGroupsTool>();
        services.AddScoped<IAiTool, ListRelationTypesTool>();
        services.AddScoped<IAiTool, CreateProjectTool>();
        services.AddScoped<IAiTool, CreateBoardTool>();
        services.AddScoped<IAiTool, CreateStatusTool>();
        services.AddScoped<IAiTool, MoveTaskToBoardGroupTool>();
        services.AddScoped<IAiTool, LinkTasksTool>();
        services.AddScoped<IAiTool, GetTaskTool>();
        services.AddScoped<IAiTool, ListTaskCommentsTool>();
        services.AddScoped<IAiTool, ListTaskRelationsTool>();
        services.AddScoped<IAiTool, GetCurrentSprintTool>();
        services.AddScoped<IAiTool, CreateSprintTool>();
        services.AddScoped<IAiTool, UpdateSprintTool>();
        services.AddScoped<IAiTool, StartSprintTool>();
        services.AddScoped<IAiTool, CompleteSprintTool>();
        services.AddScoped<IAiTool, CancelSprintTool>();
        services.AddScoped<IAiTool, DeleteSprintTool>();
        services.AddScoped<IAiTool, AddTasksToSprintTool>();
        services.AddScoped<IAiTool, RemoveTaskFromSprintTool>();
        services.AddScoped<IAiTool, UpdateProjectTool>();
        services.AddScoped<IAiTool, ResolveTaskFlagTool>();
        services.AddScoped<IAiTool, CreateBoardGroupTool>();

        services.AddScoped<IAiChangeHandler, CreateTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, AssignTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, MoveTaskToSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, SetTaskTagsChangeHandler>();
        services.AddScoped<IAiChangeHandler, AddTaskCommentChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateProjectChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateBoardChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateStatusChangeHandler>();
        services.AddScoped<IAiChangeHandler, MoveTaskToBoardGroupChangeHandler>();
        services.AddScoped<IAiChangeHandler, LinkTasksChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, StartSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, CompleteSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, CancelSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, DeleteSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, AddTasksToSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, RemoveTaskFromSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateProjectChangeHandler>();
        services.AddScoped<IAiChangeHandler, ResolveTaskFlagChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateBoardGroupChangeHandler>();
        services.AddScoped<IAiChangeSetBuilder, AiChangeSetBuilder>();
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();

        services.AddScoped<IAiConversationRunner, AiConversationRunner>();
        services.AddScoped<IAiSystemPromptBuilder, AiSystemPromptBuilder>();
        services.AddScoped<IAiTitleGenerator, AiTitleGenerator>();
        services.AddScoped<IAiConversationService, AiConversationService>();
        services.AddScoped<IAiChangeSetApplier, AiChangeSetApplier>();

        return services;
    }
}
