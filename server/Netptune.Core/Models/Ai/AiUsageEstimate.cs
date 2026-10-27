namespace Netptune.Core.Models.Ai;

// Providers report usage only with the finished reply, so a reply cut short is booked from its length.
public static class AiUsageEstimate
{
    private const int CharactersPerToken = 4;

    public static AiUsage For(AiChatRequest request, int streamedCharacters)
    {
        var promptCharacters = request.SystemPrompt.Length + request.Messages.Sum(message => message.PromptText.Length);

        return new AiUsage
        {
            InputTokens = TokensFor(promptCharacters),
            OutputTokens = TokensFor(streamedCharacters),
        };
    }

    private static int TokensFor(int characters)
    {
        return (characters + CharactersPerToken - 1) / CharactersPerToken;
    }
}
