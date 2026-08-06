namespace Netptune.Transfer;

public sealed class EntityRefDisambiguator
{
    public const char DisambiguationSeparator = '~';

    private readonly HashSet<EntityRef> Issued = [];
    private readonly Dictionary<EntityRef, int> Occurrences = [];

    public int DisambiguatedCount { get; private set; }

    public EntityRef Disambiguate(EntityRef entityRef)
    {
        var isFirstUse = Issued.Add(entityRef);

        if (isFirstUse)
        {
            Occurrences[entityRef] = 1;

            return entityRef;
        }

        var occurrence = Occurrences[entityRef];
        var candidate = entityRef;
        var isCandidateAvailable = false;

        while (!isCandidateAvailable)
        {
            occurrence++;
            candidate = entityRef with { Value = $"{entityRef.Value}{DisambiguationSeparator}{occurrence}" };
            isCandidateAvailable = Issued.Add(candidate);
        }

        Occurrences[entityRef] = occurrence;
        DisambiguatedCount++;

        return candidate;
    }
}
