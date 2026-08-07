using Netptune.Transfer.Enums;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;

namespace Netptune.Import;

public sealed record SuggestedMapping
{
    public required ImportMappingModel Mapping { get; init; }

    public IReadOnlyList<int> UnmappedColumns { get; init; } = [];
}

public sealed class ImportMappingSuggester
{
    public const double MinimumConfidence = 0.55;

    private const double ExactNameScore = 1.0;
    private const double SynonymScore = 0.9;
    private const double SimilarityFloor = 0.75;
    private const double ValueShapeBonus = 0.15;

    public SuggestedMapping Suggest(
        string recordTypeKey,
        ImportSourceProfile profile,
        ImportSuggestionVocabulary? vocabulary = null)
    {
        var recordType = TransferFieldCatalog.FindRecordType(recordTypeKey);

        if (recordType is null)
        {
            return Empty(recordTypeKey, profile);
        }

        var candidates = ScoreCandidates(recordType, profile, vocabulary)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.ColumnIndex)
            .ToList();
        var boundFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var boundColumns = new HashSet<int>();
        var bindings = new List<ImportFieldBinding>();

        foreach (var candidate in candidates)
        {
            var isAvailable = !boundFields.Contains(candidate.Field.Key) && !boundColumns.Contains(candidate.ColumnIndex);

            if (!isAvailable || candidate.Score < MinimumConfidence)
            {
                continue;
            }

            boundFields.Add(candidate.Field.Key);
            boundColumns.Add(candidate.ColumnIndex);

            bindings.Add(new ImportFieldBinding
            {
                FieldKey = candidate.Field.Key,
                ColumnIndex = candidate.ColumnIndex,
                Confidence = Math.Round(candidate.Score, 2),
                Origin = ImportBindingOrigin.Heuristic,
                ValueMap = BuildValueMap(candidate, vocabulary),
            });
        }

        var unmapped = profile.Columns
            .Select(column => column.Index)
            .Where(index => !boundColumns.Contains(index))
            .ToList();

        return new SuggestedMapping
        {
            Mapping = new ImportMappingModel
            {
                RecordType = recordType.Key,
                Bindings = bindings.OrderBy(binding => binding.ColumnIndex).ToList(),
            },
            UnmappedColumns = unmapped,
        };
    }

    private static IEnumerable<Candidate> ScoreCandidates(
        TransferRecordType recordType,
        ImportSourceProfile profile,
        ImportSuggestionVocabulary? vocabulary)
    {
        foreach (var column in profile.Columns)
        {
            foreach (var field in recordType.Fields)
            {
                var score = ScorePair(column, field, vocabulary);

                if (score <= 0)
                {
                    continue;
                }

                yield return new Candidate(column, field, Math.Min(score, 1.0));
            }
        }
    }

    private static double ScorePair(ImportSourceColumn column, TransferField field, ImportSuggestionVocabulary? vocabulary)
    {
        var name = Normalize(column.Name);
        var nameScore = NameScore(name, field);

        if (nameScore <= 0)
        {
            return 0;
        }

        return nameScore + ShapeBonus(column, field, vocabulary);
    }

    private static double NameScore(string name, TransferField field)
    {
        if (name.Length == 0)
        {
            return 0;
        }

        if (name == Normalize(field.Name) || name == Normalize(LocalKey(field)))
        {
            return ExactNameScore;
        }

        var isSynonym = field.Synonyms.Any(synonym => Normalize(synonym) == name);

        if (isSynonym)
        {
            return SynonymScore;
        }

        var best = field.Synonyms
            .Select(Normalize)
            .Append(Normalize(field.Name))
            .Append(Normalize(LocalKey(field)))
            .Max(candidate => Similarity(name, candidate));

        if (best < SimilarityFloor)
        {
            return 0;
        }

        return 0.5 + ((best - SimilarityFloor) / (1 - SimilarityFloor) * 0.3);
    }

    private static double ShapeBonus(ImportSourceColumn column, TransferField field, ImportSuggestionVocabulary? vocabulary)
    {
        if (column.InferredType == field.ValueType)
        {
            return ValueShapeBonus;
        }

        var known = vocabulary?.Match(field, column.SampleValues) ?? 0;

        return known >= 0.9 ? ValueShapeBonus : 0;
    }

    private static Dictionary<string, string> BuildValueMap(Candidate candidate, ImportSuggestionVocabulary? vocabulary)
    {
        if (vocabulary is null)
        {
            return [];
        }

        return vocabulary.SuggestValueMap(candidate.Field, candidate.Column.SampleValues);
    }

    private static SuggestedMapping Empty(string recordTypeKey, ImportSourceProfile profile)
    {
        return new SuggestedMapping
        {
            Mapping = new ImportMappingModel { RecordType = recordTypeKey },
            UnmappedColumns = profile.Columns.Select(column => column.Index).ToList(),
        };
    }

    private static string LocalKey(TransferField field)
    {
        var separatorIndex = field.Key.IndexOf('.');

        return separatorIndex < 0 ? field.Key : field.Key[(separatorIndex + 1)..];
    }

    internal static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    // Normalised Levenshtein similarity in [0, 1].
    internal static double Similarity(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        if (left == right)
        {
            return 1;
        }

        var distance = Distance(left, right);
        var longest = Math.Max(left.Length, right.Length);

        return 1.0 - ((double)distance / longest);
    }

    private static int Distance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var index = 0; index <= right.Length; index++)
        {
            previous[index] = index;
        }

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;

            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitution = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;

                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private sealed record Candidate(ImportSourceColumn Column, TransferField Field, double Score)
    {
        public int ColumnIndex => Column.Index;
    }
}
