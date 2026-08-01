using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiChangeSetBuilder : IAiChangeSetBuilder
{
    private readonly List<AiChangeDraft> Drafts = [];

    private int RefCounter;

    public IReadOnlyList<AiChangeDraft> Changes => Drafts;

    public string CreateRefKey()
    {
        RefCounter += 1;

        return $"ref:{RefCounter}";
    }

    public void Add(AiChangeDraft draft)
    {
        Drafts.Add(draft);
    }
}
