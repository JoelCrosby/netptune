using System.Runtime.CompilerServices;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Providers;
using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
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
        var gatheredCharacters = 0;
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
            var streamedCharacters = 0;

            // Usage only arrives with a finished reply. A call stopped, timed out or broken off part way was
            // still paid for, and only a finally block runs once the caller has stopped listening.
            try
            {
                await foreach (var providerEvent in provider.Stream(request, context.ApiKey, cancellationToken))
                {
                    if (providerEvent.TextDelta is not null)
                    {
                        streamedCharacters += providerEvent.TextDelta.Length;

                        yield return AiStreamEvent.Delta(providerEvent.TextDelta);
                    }

                    if (providerEvent.CompletedTurn is not null)
                    {
                        turn = providerEvent.CompletedTurn;
                    }
                }
            }
            finally
            {
                var wasCutShort = turn is null
                    && (streamedCharacters > 0 || cancellationToken.IsCancellationRequested);

                if (wasCutShort)
                {
                    context.Turns.Add(new AiChatTurn { Usage = AiUsageEstimate.For(request, streamedCharacters) });
                }
            }

            if (turn is null)
            {
                yield return AiStreamEvent.Failed(AppText.AiErrorProviderNoResponse);

                yield break;
            }

            context.Turns.Add(turn);

            spent = spent.Add(turn.Usage);

            var hasReportedUsage = spent.TotalTokens > 0;
            var usage = AiTokenUsageViewModel.From(spent).WithCost(context.Model);

            if (hasReportedUsage)
            {
                yield return AiStreamEvent.TurnUsage(usage);
            }

            var hasToolCalls = turn.ToolCalls.Count > 0;
            var hitOutputLimit = AiFinishReasons.IsOutputLimit(turn.FinishReason);

            if (!hasToolCalls && hitOutputLimit)
            {
                yield return AiStreamEvent.Failed(AppText.AiErrorReplyHitOutputLimit);

                yield break;
            }

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
                var label = DescribeCall(call, availableTools);

                yield return AiStreamEvent.ToolStarted(label);

                var result = await ExecuteTool(call, availableTools, context.Permissions, cancellationToken);

                context.Invocations.Add(new AiToolInvocationRecord
                {
                    ToolName = call.Name,
                    Label = label,
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

                yield return AiStreamEvent.ToolCompleted(label);
            }

            messages.Add(new AiChatMessage { Role = AiMessageRole.Tool, ToolResults = results });

            var question = Questions.Pending;

            if (question is not null)
            {
                yield return AiStreamEvent.QuestionAsked(question);

                yield return new AiStreamEvent { Type = AiStreamEventType.TurnCompleted };

                yield break;
            }

            gatheredCharacters += results.Sum(result => result.Content.Length);

            var isOverTurnBudget = gatheredCharacters >= Options.MaxTurnCharacters;

            if (isOverTurnBudget)
            {
                yield return AiStreamEvent.Failed(AppText.AiErrorTurnBudgetReached);

                yield break;
            }

            var isOverSpendCap = context.SpendRemaining is { } remaining && usage.Cost >= remaining;

            if (isOverSpendCap)
            {
                yield return AiStreamEvent.Failed(AppText.AiSpendCapReachedDuringTurn);

                yield break;
            }
        }

        var roundsReached = Localized.Format(AppText.AiErrorToolRoundsReached, Options.MaxToolIterations);

        yield return AiStreamEvent.Failed(roundsReached);
    }

    private async Task<AiToolExecution> ExecuteTool(
        AiToolCall call,
        IReadOnlyList<IAiTool> availableTools,
        IReadOnlySet<string> permissions,
        CancellationToken cancellationToken)
    {
        var tool = availableTools.FirstOrDefault(item => string.Equals(item.Name, call.Name, StringComparison.Ordinal));

        if (tool is null)
        {
            return AiToolExecution.Failed($"Tool {call.Name} is not available.");
        }

        var required = tool.GetRequiredPermissions(call.Arguments.RootElement);
        var missing = required.Where(permission => !permissions.Contains(permission)).ToList();

        if (missing.Count > 0)
        {
            return AiToolExecution.Failed(
                $"The user does not have permission to do this with {call.Name}: {string.Join(", ", missing)}.");
        }

        try
        {
            var execution = await tool.Execute(call.Arguments.RootElement, cancellationToken);

            return Truncate(execution);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AiToolExecution.Failed($"Tool {call.Name} timed out.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AiToolExecution.Failed($"Tool {call.Name} failed: {exception.Message}");
        }
    }

    private static string DescribeCall(AiToolCall call, IReadOnlyList<IAiTool> availableTools)
    {
        var tool = availableTools.FirstOrDefault(item => string.Equals(item.Name, call.Name, StringComparison.Ordinal));

        return tool?.DescribeCall(call.Arguments.RootElement) ?? call.Name;
    }

    private AiToolExecution Truncate(AiToolExecution execution)
    {
        var content = AiToolJson.Relax(execution.Content);
        var isWithinLimit = content.Length <= Options.MaxToolResultCharacters;

        if (isWithinLimit)
        {
            return execution with { Content = content };
        }

        var trimmed = content[..Options.MaxToolResultCharacters];

        return execution with
        {
            Content = $"{trimmed}\n\n[result truncated]",
            Truncated = true,
        };
    }

    private IReadOnlyList<IAiTool> GetAvailableTools(IReadOnlySet<string> permissions)
    {
        return Tools.All
            .Where(tool => tool.IsAvailable(permissions))
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
