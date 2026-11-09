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
        var availableTools = GetAvailableTools(context);
        var definitions = availableTools.Select(CreateDefinition).ToList();
        var messages = new List<AiChatMessage>(context.History);
        var iteration = 0;
        var gatheredCharacters = 0;
        var hasCorrectedReply = false;
        var spent = new AiUsage();
        var maxToolIterations = context.MaxToolIterations ?? Options.MaxToolIterations;
        var maxTurnCharacters = context.MaxTurnCharacters ?? Options.MaxTurnCharacters;
        var thinkingDisplay = context.ShowsThinking
            ? AiModels.ThinkingDisplayFor(context.Model)
            : AiThinkingDisplay.None;

        while (iteration < maxToolIterations)
        {
            iteration += 1;

            var request = new AiChatRequest
            {
                Model = context.Model,
                SystemPrompt = context.SystemPrompt,
                Messages = messages,
                Tools = definitions,
                MaxOutputTokens = context.MaxOutputTokens ?? Options.MaxOutputTokens,
                Effort = context.Effort,
                ThinkingDisplay = thinkingDisplay,
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

                    var isShownThinking = context.ShowsThinking && providerEvent.ThinkingDelta is not null;

                    if (isShownThinking)
                    {
                        yield return AiStreamEvent.Thinking(providerEvent.ThinkingDelta!);
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
                yield return AiStreamEvent.Failed(AppText.AiErrorReplyHitOutputLimit, AiRunFailureCodes.OutputLimit);

                yield break;
            }

            if (!hasToolCalls)
            {
                var hasRoundLeft = iteration < maxToolIterations;
                var canCorrect = !hasCorrectedReply && hasRoundLeft && !IsOverSpendCap(context, usage);
                var correction = canCorrect ? FindCorrection(context, turn) : null;

                if (correction is null)
                {
                    yield return new AiStreamEvent { Type = AiStreamEventType.TurnCompleted };

                    yield break;
                }

                hasCorrectedReply = true;

                messages.Add(new AiChatMessage
                {
                    Role = AiMessageRole.Assistant,
                    Text = turn.Text,
                    ProviderPayload = turn.ProviderPayload,
                });

                messages.Add(new AiChatMessage
                {
                    Role = AiMessageRole.User,
                    Text = correction,
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

            foreach (var (position, call) in turn.ToolCalls.Index())
            {
                var label = DescribeCall(call, availableTools);
                var isFirstInTurn = position == 0;

                yield return AiStreamEvent.ToolStarted(label, isFirstInTurn);

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

                yield return AiStreamEvent.ToolCompleted(label, result.IsError);
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

            var isOverTurnBudget = gatheredCharacters >= maxTurnCharacters;

            if (isOverTurnBudget)
            {
                yield return AiStreamEvent.Failed(AppText.AiErrorTurnBudgetReached, AiRunFailureCodes.ToolBudget);

                yield break;
            }

            if (IsOverSpendCap(context, usage))
            {
                yield return AiStreamEvent.Failed(AppText.AiSpendCapReachedDuringTurn, AiRunFailureCodes.SpendCap);

                yield break;
            }
        }

        var roundsReached = Localized.Format(AppText.AiErrorToolRoundsReached, maxToolIterations);

        yield return AiStreamEvent.Failed(roundsReached, AiRunFailureCodes.ToolRounds);
    }

    private static bool IsOverSpendCap(AiRunContext context, AiTokenUsageViewModel usage)
    {
        return context.SpendRemaining is { } remaining && usage.Cost >= remaining;
    }

    private string? FindCorrection(AiRunContext context, AiChatTurn turn)
    {
        var isUnbackedClaim = context.CorrectsProposalClaims
            && AiProposalClaim.IsUnbacked(turn.Text, ChangeSet.Changes.Count);

        return isUnbackedClaim ? AiProposalClaim.Correction : context.ReplyCorrection?.Invoke(turn);
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

    private IReadOnlyList<IAiTool> GetAvailableTools(AiRunContext context)
    {
        return Tools.All
            .Where(tool => IsOffered(tool, context))
            .ToList();
    }

    private static bool IsOffered(IAiTool tool, AiRunContext context)
    {
        var isAllowedKind = context.ToolKinds.Contains(tool.Kind);

        return isAllowedKind && tool.IsAvailable(context.Permissions);
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
