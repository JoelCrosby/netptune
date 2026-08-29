using System.Runtime.CompilerServices;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiConversationRunner : IAiConversationRunner
{
    private readonly IAiChatProviderFactory ProviderFactory;
    private readonly IAiToolRegistry Tools;
    private readonly IAiChangeSetBuilder ChangeSet;
    private readonly IAiQuestionSink Questions;
    private readonly AiOptions Options;

    public AiConversationRunner(
        IAiChatProviderFactory providerFactory,
        IAiToolRegistry tools,
        IAiChangeSetBuilder changeSet,
        IAiQuestionSink questions,
        IOptions<AiOptions> options)
    {
        ProviderFactory = providerFactory;
        Tools = tools;
        ChangeSet = changeSet;
        Questions = questions;
        Options = options.Value;
    }

    public async IAsyncEnumerable<AiStreamEvent> Run(
        AiRunContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var provider = ProviderFactory.Resolve(context.Provider);
        var availableTools = GetAvailableTools(context.Permissions);
        var definitions = availableTools.Select(CreateDefinition).ToList();
        var messages = new List<AiChatMessage>(context.History);
        var iteration = 0;
        var hasCorrectedClaim = false;
        var spent = new AiUsage();

        while (iteration < Options.MaxToolIterations)
        {
            iteration += 1;

            var request = new AiChatRequest
            {
                Model = context.Model,
                SystemPrompt = context.SystemPrompt,
                Messages = messages,
                Tools = definitions,
                MaxOutputTokens = Options.MaxOutputTokens,
                Effort = context.Effort,
            };

            AiChatTurn? turn = null;

            await foreach (var providerEvent in provider.Stream(request, context.ApiKey, cancellationToken))
            {
                if (providerEvent.TextDelta is not null)
                {
                    yield return AiStreamEvent.Delta(providerEvent.TextDelta);
                }

                if (providerEvent.CompletedTurn is not null)
                {
                    turn = providerEvent.CompletedTurn;
                }
            }

            if (turn is null)
            {
                yield return AiStreamEvent.Failed("The provider returned no response.");

                yield break;
            }

            context.Turns.Add(turn);

            spent = spent.Add(turn.Usage);

            var hasReportedUsage = spent.TotalTokens > 0;

            if (hasReportedUsage)
            {
                yield return AiStreamEvent.TurnUsage(AiTokenUsageViewModel.From(spent).WithCost(context.Model));
            }

            var hasToolCalls = turn.ToolCalls.Count > 0;

            if (!hasToolCalls)
            {
                var isUnbackedClaim = !hasCorrectedClaim
                    && AiProposalClaim.IsUnbacked(turn.Text, ChangeSet.Changes.Count);

                if (!isUnbackedClaim)
                {
                    yield return new AiStreamEvent { Type = AiStreamEventType.TurnCompleted };

                    yield break;
                }

                hasCorrectedClaim = true;

                messages.Add(new AiChatMessage
                {
                    Role = AiMessageRole.Assistant,
                    Text = turn.Text,
                    ProviderPayload = turn.ProviderPayload,
                });

                messages.Add(new AiChatMessage
                {
                    Role = AiMessageRole.User,
                    Text = AiProposalClaim.Correction,
                });

                yield return AiStreamEvent.ReplyReset();

                continue;
            }

            messages.Add(new AiChatMessage
            {
                Role = AiMessageRole.Assistant,
                Text = turn.Text,
                ToolCalls = turn.ToolCalls,
                ProviderPayload = turn.ProviderPayload,
            });

            var results = new List<AiToolResult>();

            foreach (var call in turn.ToolCalls)
            {
                yield return AiStreamEvent.ToolStarted(call.Name);

                var result = await ExecuteTool(call, availableTools, cancellationToken);

                context.Invocations.Add(new AiToolInvocationRecord
                {
                    ToolName = call.Name,
                    Arguments = call.Arguments,
                    Result = result.Content,
                    IsError = result.IsError,
                    Truncated = result.Truncated,
                });

                results.Add(new AiToolResult
                {
                    ToolCallId = call.Id,
                    Content = result.Content,
                    IsError = result.IsError,
                });

                yield return AiStreamEvent.ToolCompleted(call.Name);
            }

            messages.Add(new AiChatMessage { Role = AiMessageRole.Tool, ToolResults = results });

            var question = Questions.Pending;

            if (question is not null)
            {
                yield return AiStreamEvent.QuestionAsked(question);

                yield return new AiStreamEvent { Type = AiStreamEventType.TurnCompleted };

                yield break;
            }
        }

        yield return AiStreamEvent.Failed("The assistant stopped after reaching the tool call limit.");
    }

    private async Task<AiToolExecution> ExecuteTool(
        AiToolCall call,
        IReadOnlyList<IAiTool> availableTools,
        CancellationToken cancellationToken)
    {
        var tool = availableTools.FirstOrDefault(item => string.Equals(item.Name, call.Name, StringComparison.Ordinal));

        if (tool is null)
        {
            return AiToolExecution.Failed($"Tool {call.Name} is not available.");
        }

        try
        {
            var execution = await tool.Execute(call.Arguments.RootElement, cancellationToken);

            return Truncate(execution);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AiToolExecution.Failed($"Tool {call.Name} failed: {exception.Message}");
        }
    }

    private AiToolExecution Truncate(AiToolExecution execution)
    {
        var isWithinLimit = execution.Content.Length <= Options.MaxToolResultCharacters;

        if (isWithinLimit)
        {
            return execution;
        }

        var trimmed = execution.Content[..Options.MaxToolResultCharacters];

        return execution with
        {
            Content = $"{trimmed}\n\n[result truncated]",
            Truncated = true,
        };
    }

    private IReadOnlyList<IAiTool> GetAvailableTools(IReadOnlySet<string> permissions)
    {
        return Tools.All
            .Where(tool => tool.RequiredPermissions.All(permissions.Contains))
            .ToList();
    }

    private static AiToolDefinition CreateDefinition(IAiTool tool)
    {
        return new AiToolDefinition
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.InputSchema,
        };
    }
}
