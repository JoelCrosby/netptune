using Netptune.Transfer.Enums;
using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Netptune.Transfer.Services;
using Netptune.Transfer.Mapping;

namespace Netptune.Import;

public sealed class CsvImportSourceReader : IImportSourceReader
{
    private const int DelimiterSniffLines = 20;

    private static readonly char[] CandidateDelimiters = [',', ';', '\t', '|'];

    public IReadOnlySet<ImportSourceKind> Kinds { get; } =
        new[] { ImportSourceKind.Csv, ImportSourceKind.Tsv }.ToFrozenSet();

    public bool CanRead(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension is ".csv" or ".tsv" or ".txt";
    }

    public async Task<ImportSourceProfile> Profile(Stream source, ImportReadOptions options, CancellationToken cancellationToken = default)
    {
        var encoding = DetectEncoding(source);
        var delimiter = options.Delimiter ?? SniffDelimiter(source, encoding);
        var readOptions = options with { Delimiter = delimiter };
        var profiler = new ImportColumnProfiler();
        var isFirst = true;

        await foreach (var row in ReadRows(source, readOptions, cancellationToken))
        {
            if (isFirst)
            {
                profiler.SetHeaders(ImportColumnProfiler.HeaderNames(row, options.HasHeaderRow));
                isFirst = false;

                if (options.HasHeaderRow)
                {
                    continue;
                }
            }

            profiler.Add(row);
        }

        return new ImportSourceProfile
        {
            Kind = ImportSourceKind.Csv,
            Encoding = encoding.WebName,
            Delimiter = delimiter,
            HasHeaderRow = options.HasHeaderRow,
            EstimatedRowCount = profiler.RowsSeen,
            Columns = profiler.ToColumns(),
        };
    }

    public async IAsyncEnumerable<ImportRow> ReadRows(
        Stream source,
        ImportReadOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        source.Seek(0, SeekOrigin.Begin);

        var encoding = DetectEncoding(source);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = (options.Delimiter ?? ',').ToString(),
            HasHeaderRecord = false,
            BadDataFound = null,
            MissingFieldFound = null,
            DetectColumnCountChanges = false,
        };

        using var reader = new StreamReader(source, encoding, leaveOpen: true);
        using var csv = new CsvReader(reader, configuration);

        var rowNumber = 0;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = new string?[csv.Parser.Count];

            for (var index = 0; index < csv.Parser.Count; index++)
            {
                values[index] = csv.Parser[index];
            }

            rowNumber++;

            yield return new ImportRow
            {
                RowNumber = rowNumber,
                Values = values,
            };
        }
    }

    private static Encoding DetectEncoding(Stream source)
    {
        source.Seek(0, SeekOrigin.Begin);

        var preamble = new byte[4];
        var read = source.Read(preamble, 0, 4);

        source.Seek(0, SeekOrigin.Begin);

        if (read >= 3 && preamble[0] == 0xEF && preamble[1] == 0xBB && preamble[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        if (read >= 2 && preamble[0] == 0xFF && preamble[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        if (read >= 2 && preamble[0] == 0xFE && preamble[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        return new UTF8Encoding(false);
    }

    private static char SniffDelimiter(Stream source, Encoding encoding)
    {
        source.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(source, encoding, leaveOpen: true);
        var counts = CandidateDelimiters.ToDictionary(delimiter => delimiter, _ => 0);

        for (var line = 0; line < DelimiterSniffLines; line++)
        {
            var text = reader.ReadLine();

            if (text is null)
            {
                break;
            }

            foreach (var delimiter in CandidateDelimiters)
            {
                counts[delimiter] += text.Count(character => character == delimiter);
            }
        }

        source.Seek(0, SeekOrigin.Begin);

        var best = counts.MaxBy(entry => entry.Value);

        return best.Value == 0 ? ',' : best.Key;
    }
}
