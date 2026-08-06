using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Import;

public sealed record AiMappingValidationResult
{
    public required ImportMappingModel Mapping { get; init; }

    public int DiscardedBindings { get; init; }

    public IReadOnlyList<string> DiscardReasons { get; init; } = [];

    public string? Notes { get; init; }
}

// Turns a model's proposal into a mapping the rest of the system can trust. Anything that does not
// name a real field, a real column, a real value or a real transform is dropped and counted — the
// model never writes to the database, and the user still has to press Preview.
public static class AiMappingProposalValidator
{
    public static AiMappingValidationResult Validate(
        AiMappingProposal? proposal,
        string recordTypeKey,
        ImportSourceProfile profile,
        ImportSuggestionVocabulary? vocabulary = null)
    {
        var recordType = TransferFieldCatalog.FindRecordType(recordTypeKey);
        var empty = new ImportMappingModel { RecordType = recordTypeKey };

        if (proposal is null || recordType is null)
        {
            return new AiMappingValidationResult { Mapping = empty };
        }

        var reasons = new List<string>();
        var bindings = new List<ImportFieldBinding>();
        var boundFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var boundColumns = new HashSet<int>();
        var discarded = 0;

        foreach (var candidate in proposal.Bindings)
        {
            var field = recordType.Fields.FirstOrDefault(known =>
                string.Equals(known.Key, candidate.FieldKey, StringComparison.OrdinalIgnoreCase));

            if (field is null)
            {
                discarded++;
                reasons.Add($"'{candidate.FieldKey}' is not a field of '{recordType.Key}'.");
                continue;
            }

            var columnIndex = candidate.ColumnIndex;
            var isColumnInRange = columnIndex is not null && profile.Columns.Any(column => column.Index == columnIndex);

            if (!isColumnInRange)
            {
                discarded++;
                reasons.Add($"'{field.Key}' points at column {columnIndex}, which the file does not have.");
                continue;
            }

            if (!boundFields.Add(field.Key))
            {
                discarded++;
                reasons.Add($"'{field.Key}' was proposed more than once.");
                continue;
            }

            if (!boundColumns.Add(columnIndex!.Value))
            {
                discarded++;
                reasons.Add($"Column {columnIndex} was proposed more than once.");
                continue;
            }

            bindings.Add(new ImportFieldBinding
            {
                FieldKey = field.Key,
                ColumnIndex = columnIndex,
                Transforms = ValidTransforms(candidate, reasons),
                ValueMap = ValidValueMap(candidate, field, vocabulary, reasons),
                Confidence = Math.Clamp(candidate.Confidence, 0, 1),
                Origin = ImportBindingOrigin.Assistant,
            });
        }

        return new AiMappingValidationResult
        {
            Mapping = new ImportMappingModel
            {
                RecordType = recordType.Key,
                Bindings = bindings.OrderBy(binding => binding.ColumnIndex).ToList(),
            },
            DiscardedBindings = discarded,
            DiscardReasons = reasons,
            Notes = proposal.Notes,
        };
    }

    private static List<ImportTransform> ValidTransforms(AiMappingProposalBinding candidate, List<string> reasons)
    {
        var transforms = new List<ImportTransform>();

        foreach (var transform in candidate.Transforms)
        {
            var parsed = Enum.TryParse<ImportTransformKind>(transform.Kind, true, out var kind);

            if (!parsed)
            {
                reasons.Add($"'{transform.Kind}' is not a transform this import supports.");
                continue;
            }

            transforms.Add(new ImportTransform { Kind = kind, Argument = transform.Argument });
        }

        return transforms;
    }

    private static Dictionary<string, string> ValidValueMap(
        AiMappingProposalBinding candidate,
        TransferField field,
        ImportSuggestionVocabulary? vocabulary,
        List<string> reasons)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (candidate.ValueMap.Count == 0)
        {
            return map;
        }

        var targets = Targets(field, vocabulary);

        foreach (var entry in candidate.ValueMap)
        {
            var isKnownTarget = targets.Count == 0 || targets.Contains(entry.Value);

            if (!isKnownTarget)
            {
                reasons.Add($"'{entry.Value}' is not a value '{field.Key}' can map onto.");
                continue;
            }

            map[entry.Key] = entry.Value;
        }

        return map;
    }

    private static IReadOnlySet<string> Targets(TransferField field, ImportSuggestionVocabulary? vocabulary)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        if (vocabulary is null)
        {
            return new HashSet<string>(comparer);
        }

        return field.RefType switch
        {
            EntityRefTypes.Status => vocabulary.StatusKeysByName.Values.ToHashSet(comparer),
            EntityRefTypes.Tag => vocabulary.TagNames.ToHashSet(comparer),
            EntityRefTypes.User => vocabulary.MemberEmails.ToHashSet(comparer),
            EntityRefTypes.Project => vocabulary.ProjectKeys.ToHashSet(comparer),
            _ => new HashSet<string>(comparer),
        };
    }
}
