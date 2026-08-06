using System.Globalization;
using System.Text.RegularExpressions;

using Netptune.Transfer;
using Netptune.Transfer.Mapping;

namespace Netptune.Import;

// Accumulates one column's values so every reader infers types, samples and cardinality the same way.
public sealed partial class ImportColumnProfiler(int sampleLimit = 500)
{
    private readonly List<ColumnAccumulator> Columns = [];

    public IReadOnlyList<string> Headers { get; private set; } = [];

    // Every data row seen, not just the sampled ones.
    public long RowCount { get; private set; }

    public void SetHeaders(IEnumerable<string> headers)
    {
        Headers = headers.ToList();
        Columns.Clear();
        Columns.AddRange(Headers.Select((name, index) => new ColumnAccumulator(index, name)));
    }

    public void Add(ImportRow row)
    {
        RowCount++;

        if (RowCount > sampleLimit)
        {
            return;
        }

        for (var index = 0; index < row.Values.Count && index < Columns.Count; index++)
        {
            Columns[index].Add(row.Values[index]);
        }
    }

    public List<ImportSourceColumn> ToColumns()
    {
        return Columns.Select(column => column.ToColumn()).ToList();
    }

    public static IEnumerable<string> HeaderNames(ImportRow row, bool hasHeaderRow)
    {
        if (hasHeaderRow)
        {
            return row.Values.Select((value, index) => string.IsNullOrWhiteSpace(value) ? $"Column {index + 1}" : value.Trim());
        }

        return row.Values.Select((_, index) => $"Column {index + 1}");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    private sealed class ColumnAccumulator(int index, string name)
    {
        private readonly List<string> Samples = [];
        private readonly HashSet<string> Distinct = new(StringComparer.OrdinalIgnoreCase);
        private int NonEmpty;
        private int Dates;
        private int Numbers;
        private int Booleans;
        private int Emails;

        public void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var trimmed = value.Trim();

            NonEmpty++;
            Distinct.Add(trimmed);

            if (Samples.Count < ImportSourceProfile.MaxSampleValues && !Samples.Contains(trimmed))
            {
                Samples.Add(Truncate(trimmed));
            }

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                Dates++;
            }

            if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                Numbers++;
            }

            if (bool.TryParse(trimmed, out _))
            {
                Booleans++;
            }

            if (EmailPattern().IsMatch(trimmed))
            {
                Emails++;
            }
        }

        public ImportSourceColumn ToColumn()
        {
            return new ImportSourceColumn
            {
                Index = index,
                Name = name,
                InferredType = Infer(),
                NonEmptyCount = NonEmpty,
                DistinctCount = Distinct.Count,
                SampleValues = Samples,
            };
        }

        private TransferValueType Infer()
        {
            if (NonEmpty == 0)
            {
                return TransferValueType.Text;
            }

            var majority = NonEmpty * 0.8;

            if (Booleans >= majority)
            {
                return TransferValueType.Boolean;
            }

            if (Emails >= majority)
            {
                return TransferValueType.Ref;
            }

            if (Numbers >= majority)
            {
                return TransferValueType.Decimal;
            }

            if (Dates >= majority)
            {
                return TransferValueType.DateTime;
            }

            return TransferValueType.Text;
        }

        private static string Truncate(string value)
        {
            if (value.Length <= ImportSourceProfile.MaxSampleLength)
            {
                return value;
            }

            return value[..ImportSourceProfile.MaxSampleLength];
        }
    }
}
