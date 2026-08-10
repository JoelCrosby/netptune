namespace Netptune.Core.Models.Ai;

// The outcome of applying a change set is written into the transcript as a user message, because the
// model needs the resulting ids in its history. The client reads these prefixes back to show the
// outcome as a card rather than the raw list.
public static class AiChangeSetSummary
{
    public const string AppliedPrefix = "I applied the change set.";

    public const string UndonePrefix = "I undid the change set.";

    public static bool IsOutcome(string? text)
    {
        if (text is null)
        {
            return false;
        }

        var isApplied = text.StartsWith(AppliedPrefix, StringComparison.Ordinal);
        var isUndone = text.StartsWith(UndonePrefix, StringComparison.Ordinal);

        return isApplied || isUndone;
    }
}
