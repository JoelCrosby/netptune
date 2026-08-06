namespace Netptune.Transfer.Mapping;

// The workspace values a suggested mapping has to land on. Handed to the suggester so it can break
// ties by value shape and pre-fill value maps, without the suggester itself touching the database.
public sealed record ImportSuggestionVocabulary
{
    public IReadOnlyDictionary<string, string> StatusKeysByName { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> TagNames { get; init; } = [];

    public IReadOnlyList<string> MemberEmails { get; init; } = [];

    public IReadOnlyList<string> BoardGroupNames { get; init; } = [];

    public IReadOnlyList<string> ProjectKeys { get; init; } = [];

    // The share of sample values this field's vocabulary already recognises, in [0, 1].
    public double Match(TransferField field, IReadOnlyList<string> sampleValues)
    {
        if (sampleValues.Count == 0)
        {
            return 0;
        }

        var known = Values(field);

        if (known.Count == 0)
        {
            return 0;
        }

        var matched = sampleValues.Count(value => known.Contains(value.Trim()));

        return (double)matched / sampleValues.Count;
    }

    public Dictionary<string, string> SuggestValueMap(TransferField field, IReadOnlyList<string> sampleValues)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var isStatus = field.RefType == EntityRefTypes.Status;

        if (!isStatus)
        {
            return map;
        }

        foreach (var value in sampleValues)
        {
            var trimmed = value.Trim();
            var found = StatusKeysByName.TryGetValue(trimmed, out var statusKey);
            var isRename = found && statusKey is not null && !string.Equals(statusKey, trimmed, StringComparison.OrdinalIgnoreCase);

            if (isRename)
            {
                map[trimmed] = statusKey!;
            }
        }

        return map;
    }

    private IReadOnlySet<string> Values(TransferField field)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        return field.RefType switch
        {
            EntityRefTypes.Status => StatusKeysByName.Keys
                .Concat(StatusKeysByName.Values)
                .ToHashSet(comparer),
            EntityRefTypes.Tag => TagNames.ToHashSet(comparer),
            EntityRefTypes.User => MemberEmails.ToHashSet(comparer),
            EntityRefTypes.BoardGroup => BoardGroupNames.ToHashSet(comparer),
            EntityRefTypes.Project => ProjectKeys.ToHashSet(comparer),
            _ => new HashSet<string>(comparer),
        };
    }
}
