namespace Netptune.Core.Models.Ai;

public sealed record AiQuestionOption
{
    public required string Label { get; init; }

    public string? Description { get; init; }
}

public sealed record AiQuestion
{
    public Guid Id { get; init; }

    public required string Text { get; init; }

    public string? Header { get; init; }

    public List<AiQuestionOption> Options { get; init; } = [];

    public bool MultiSelect { get; init; }
}

// The answer is written into the transcript as a user message, because the model reads its next turn
// from the message history alone. The structured form rides alongside so the client can show the
// question as an answered card rather than the sentence the model sees.
public sealed record AiQuestionAnswer
{
    public Guid QuestionId { get; init; }

    public List<string> SelectedLabels { get; init; } = [];

    public string? Text { get; init; }

    public string Describe(AiQuestion question)
    {
        var hasText = !string.IsNullOrWhiteSpace(Text);

        if (hasText)
        {
            return $"Answering “{question.Text}” in their own words: {Text!.Trim()}";
        }

        var labels = string.Join(", ", SelectedLabels);

        return $"Answering “{question.Text}”: {labels}";
    }
}
