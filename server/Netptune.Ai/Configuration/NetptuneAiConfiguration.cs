using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Providers;
using Netptune.Ai.Tools;
using Netptune.Core.Services.Ai;
using Netptune.Transfer.Services;

namespace Netptune.Ai.Configuration;

public static class NetptuneAiConfiguration
{
    public static IServiceCollection AddNetptuneAi(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AiOptions.SectionName);

        services.AddOptions<AiOptions>()
            .Bind(section)
            .Validate(
                options => options.MaxToolIterations is >= 1 and <= AiOptions.ToolIterationsCeiling,
                $"Ai:MaxToolIterations must be between 1 and {AiOptions.ToolIterationsCeiling}.")
            .Validate(
                options => options.TurnTimeoutSeconds is >= 60 and <= AiOptions.TurnTimeoutCeilingSeconds,
                $"Ai:TurnTimeoutSeconds must be between 60 and {AiOptions.TurnTimeoutCeilingSeconds}.")
            .Validate(
                options => options.MaxTurnCharacters >= options.MaxToolResultCharacters,
                "Ai:MaxTurnCharacters must be at least Ai:MaxToolResultCharacters.")
            .ValidateOnStart();

        var webOptions = section.GetSection(nameof(AiOptions.Web)).Get<AiWebOptions>() ?? new AiWebOptions();

        services.AddNetptuneAiWeb(webOptions);

        services.AddSingleton<IAiChatProvider, AnthropicChatProvider>();
        services.AddSingleton<IAiChatProvider, OpenAiChatProvider>();
        services.AddSingleton<IAiChatProviderFactory, AiChatProviderFactory>();
        services.AddSingleton<IAiCancellationRegistry, AiCancellationRegistry>();

        services.AddScoped<IAiTool, ListRecordsTool>();
        services.AddScoped<IAiTool, SearchTasksTool>();
        services.AddScoped<IAiTool, CreateTaskTool>();
        services.AddScoped<IAiTool, UpdateTaskTool>();
        services.AddScoped<IAiTool, AddTaskCommentTool>();
        services.AddScoped<IAiTool, CreateProjectTool>();
        services.AddScoped<IAiTool, BoardChangeTool>();
        services.AddScoped<IAiTool, CreateStatusTool>();
        services.AddScoped<IAiTool, UpdateStatusTool>();
        services.AddScoped<IAiTool, LinkTasksTool>();
        services.AddScoped<IAiTool, GetTaskTool>();
        services.AddScoped<IAiTool, ListRelationsTool>();
        services.AddScoped<IAiTool, GetCurrentSprintTool>();
        services.AddScoped<IAiTool, CreateSprintTool>();
        services.AddScoped<IAiTool, UpdateSprintTool>();
        services.AddScoped<IAiTool, SprintTransitionTool>();
        services.AddScoped<IAiTool, SetTaskSprintTool>();
        services.AddScoped<IAiTool, UpdateProjectTool>();
        services.AddScoped<IAiTool, ResolveTaskFlagTool>();
        services.AddScoped<IAiTool, CreateTagTool>();
        services.AddScoped<IAiTool, DeleteTaskTool>();
        services.AddScoped<IAiTool, BoardGroupChangeTool>();
        services.AddScoped<IAiTool, UnlinkTasksTool>();
        services.AddScoped<IAiTool, CreateRelationTypeTool>();
        services.AddScoped<IAiTool, GetReportTool>();
        services.AddScoped<IAiTool, ListAutomationsTool>();
        services.AddScoped<IAiTool, ListAutomationRunsTool>();
        services.AddScoped<IAiTool, ListWorkspaceFilesTool>();
        services.AddScoped<IAiTool, AskQuestionTool>();

        services.AddScoped<IAiChangeHandler, CreateTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, AssignTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, MoveTaskToSprintChangeHandler>();
        services.AddScoped<IAiChangeHandler, SetTaskTagsChangeHandler>();
        services.AddScoped<IAiChangeHandler, AddTaskCommentChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateProjectChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateBoardChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateStatusChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateStatusChangeHandler>();
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
        services.AddScoped<IAiChangeHandler, CreateTagChangeHandler>();
        services.AddScoped<IAiChangeHandler, DeleteTaskChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateBoardGroupChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateBoardChangeHandler>();
        services.AddScoped<IAiChangeHandler, DeleteBoardChangeHandler>();
        services.AddScoped<IAiChangeHandler, UpdateBoardGroupChangeHandler>();
        services.AddScoped<IAiChangeHandler, DeleteBoardGroupChangeHandler>();
        services.AddScoped<IAiChangeHandler, ReorderBoardGroupsChangeHandler>();
        services.AddScoped<IAiChangeHandler, UnlinkTasksChangeHandler>();
        services.AddScoped<IAiChangeHandler, CreateRelationTypeChangeHandler>();
        services.AddScoped<IAiChangeSetBuilder, AiChangeSetBuilder>();
        services.AddScoped<IAiQuestionSink, AiQuestionSink>();
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();

        services.AddScoped<IAiConversationRunner, AiConversationRunner>();
        services.AddScoped<IAiSystemPromptBuilder, AiSystemPromptBuilder>();
        services.AddScoped<IAiTitleGenerator, AiTitleGenerator>();
        services.AddScoped<IAiImportMappingAdvisor, AiImportMappingAdvisor>();
        services.AddScoped<IAiSpendService, AiSpendService>();
        services.AddScoped<IAiConversationService, AiConversationService>();
        services.AddScoped<IAiChangeSetApplier, AiChangeSetApplier>();
        services.AddScoped<IAiUndoCatalog, AiUndoCatalog>();

        return services;
    }
}
