using System.Text;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiTitleGenerator : IAiTitleGenerator
{
    public const int MaximumTitleLength = 60;

    private const int MaxTitleTokens = 64;
    private const int MaxSourceCharacters = 2000;

    private const string TitlePrompt =
        "You name conversations. Reply with a title of at most six words describing what the user asked about. "
        + "Use sentence case, no quotation marks, no trailing punctuation, and no preamble — reply with the title alone.";

    private readonly IAiChatProviderFactory ProviderFactory;

    public AiTitleGenerator(IAiChatProviderFactory providerFactory)
    {
        ProviderFactory = providerFactory;
    }

    public async Task<AiTitleResult> Generate(AiTitleRequest request, CancellationToken cancellationToken)
    {
        var provider = ProviderFactory.Resolve(request.Provider);
        var exchange = CreateExchange(request);
        var chatRequest = new AiChatRequest
        {
            Model = AiModels.TitleModelFor(request.Provider),
            SystemPrompt = TitlePrompt,
            Messages = [new AiChatMessage { Role = AiMessageRole.User, Text = exchange }],
            Tools = [],
            MaxOutputTokens = MaxTitleTokens,
        };

        var text = new StringBuilder();
        var usage = new AiUsage();

        await foreach (var providerEvent in provider.Stream(chatRequest, request.ApiKey, cancellationToken))
        {
            var turn = providerEvent.CompletedTurn;

            if (turn is null)
            {
                continue;
            }

            text.Append(turn.Text);
            usage = turn.Usage;
        }

        return new AiTitleResult { Title = Sanitise(text.ToString()), Usage = usage };
    }

    public static string? Sanitise(string? raw)
    {
        var hasText = !string.IsNullOrWhiteSpace(raw);

        if (!hasText)
        {
            return null;
        }

        var collapsed = string.Join(' ', raw!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var trimmed = collapsed.Trim('"', '“', '”', '\'', '.', ' ');

        if (trimmed.Length == 0)
        {
            return null;
        }

        var isShort = trimmed.Length <= MaximumTitleLength;

        if (isShort)
        {
            return trimmed;
        }

        return $"{trimmed[..MaximumTitleLength].TrimEnd()}…";
    }

    private static string CreateExchange(AiTitleRequest request)
    {
        var builder = new StringBuilder();

        builder.AppendLine("User:");
        builder.AppendLine(Truncate(request.UserMessage));

        var hasReply = !string.IsNullOrWhiteSpace(request.AssistantMessage);

        if (hasReply)
        {
            builder.AppendLine();
            builder.AppendLine("Assistant:");
            builder.AppendLine(Truncate(request.AssistantMessage));
        }

        return builder.ToString();
    }

    private static string Truncate(string value)
    {
        var isShort = value.Length <= MaxSourceCharacters;

        if (isShort)
        {
            return value;
        }

        return value[..MaxSourceCharacters];
    }
}
