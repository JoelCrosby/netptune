using Netptune.Transfer.Enums;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Netptune.Transfer.Services;
using Netptune.Transfer.Mapping;

namespace Netptune.Import;

// Reads a JSON document or an NDJSON stream as rows. Object keys become columns, in the order the
// first few records introduce them, so a record that omits a key still lines up with the others.
public sealed class JsonImportSourceReader : IImportSourceReader
{
    private const int HeaderScanLimit = 100;

    private readonly bool IsNewlineDelimited;

    public JsonImportSourceReader(bool isNewlineDelimited)
    {
        IsNewlineDelimited = isNewlineDelimited;
        Kinds = isNewlineDelimited
            ? new[] { ImportSourceKind.Ndjson }.ToFrozenSet()
            : new[] { ImportSourceKind.Json }.ToFrozenSet();
    }

    public IReadOnlySet<ImportSourceKind> Kinds { get; }

    public bool CanRead(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (IsNewlineDelimited)
        {
            return extension is ".ndjson" or ".jsonl";
        }

        return extension is ".json";
    }

    public async Task<ImportSourceProfile> Profile(Stream source, ImportReadOptions options, CancellationToken cancellationToken = default)
    {
        var headers = await ReadHeaders(source, options.RowSelector, cancellationToken);
        var profiler = new ImportColumnProfiler();

        profiler.SetHeaders(headers);

        await foreach (var row in ReadRows(source, options, cancellationToken))
        {
            profiler.Add(row);
        }

        return new ImportSourceProfile
        {
            Kind = IsNewlineDelimited ? ImportSourceKind.Ndjson : ImportSourceKind.Json,
            Encoding = Encoding.UTF8.WebName,
            HasHeaderRow = false,
            EstimatedRowCount = profiler.RowsSeen,
            Columns = profiler.ToColumns(),
        };
    }

    public async IAsyncEnumerable<ImportRow> ReadRows(
        Stream source,
        ImportReadOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // A whole document is parsed once and both the header order and the rows are taken from that
        // one parse. Reading headers through the shared element iterator would parse the file twice,
        // and JsonDocument.ParseAsync buffers all of it.
        if (!IsNewlineDelimited)
        {
            source.Seek(0, SeekOrigin.Begin);

            using var whole = await JsonDocument.ParseAsync(source, cancellationToken: cancellationToken);

            var root = FindRowArray(whole.RootElement, options.RowSelector);

            if (root is null)
            {
                yield break;
            }

            var accumulator = new HeaderAccumulator();

            foreach (var element in root.Value.EnumerateArray())
            {
                if (accumulator.IsFull)
                {
                    break;
                }

                accumulator.Add(element);
            }

            var rowNumber = 0;

            foreach (var element in root.Value.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                rowNumber++;

                yield return new ImportRow
                {
                    RowNumber = rowNumber,
                    Values = accumulator.Headers.Select(header => ReadValue(element, header)).ToList(),
                };
            }

            yield break;
        }

        // NDJSON is read line by line, so the header pass only touches the first few records.
        var headers = await ReadHeaders(source, options.RowSelector, cancellationToken);
        var line = 0;

        await foreach (var element in ReadElements(source, options.RowSelector, cancellationToken))
        {
            line++;

            yield return new ImportRow
            {
                RowNumber = line,
                Values = headers.Select(header => ReadValue(element, header)).ToList(),
            };
        }
    }

    private async Task<List<string>> ReadHeaders(Stream source, string? rowSelector, CancellationToken cancellationToken)
    {
        var accumulator = new HeaderAccumulator();

        await foreach (var element in ReadElements(source, rowSelector, cancellationToken))
        {
            if (accumulator.IsFull)
            {
                break;
            }

            accumulator.Add(element);
        }

        return accumulator.Headers;
    }

    private async IAsyncEnumerable<JsonElement> ReadElements(
        Stream source,
        string? rowSelector,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        source.Seek(0, SeekOrigin.Begin);

        if (IsNewlineDelimited)
        {
            using var reader = new StreamReader(source, Encoding.UTF8, leaveOpen: true);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parsed = TryParse(line, out var document);

                if (!parsed || document is null)
                {
                    continue;
                }

                using (document)
                {
                    yield return document.RootElement.Clone();
                }
            }

            yield break;
        }

        using var whole = await JsonDocument.ParseAsync(source, cancellationToken: cancellationToken);

        var root = FindRowArray(whole.RootElement, rowSelector);

        if (root is null)
        {
            yield break;
        }

        foreach (var element in root.Value.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return element.Clone();
        }
    }

    // Resolves the array holding the rows. An explicit selector wins; otherwise the richest object
    // array does — a Trello export leads with a thin `lists[]` before the `cards[]` that matter, so
    // "first array in the document" picks the wrong one.
    private static JsonElement? FindRowArray(JsonElement root, string? selector)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(selector))
        {
            var found = root.TryGetProperty(selector, out var selected);

            return found && selected.ValueKind == JsonValueKind.Array ? selected : null;
        }

        JsonElement? best = null;
        var bestScore = -1;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var score = DistinctKeyCount(property.Value);

            if (score > bestScore)
            {
                bestScore = score;
                best = property.Value;
            }
        }

        return best;
    }

    private static int DistinctKeyCount(JsonElement array)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var scanned = 0;

        foreach (var element in array.EnumerateArray())
        {
            if (scanned >= HeaderScanLimit)
            {
                break;
            }

            scanned++;

            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in element.EnumerateObject())
            {
                keys.Add(property.Name);
            }
        }

        return keys.Count;
    }

    private static bool TryParse(string line, out JsonDocument? document)
    {
        try
        {
            document = JsonDocument.Parse(line);

            return true;
        }
        catch (JsonException)
        {
            document = null;

            return false;
        }
    }

    private static string? ReadValue(JsonElement element, string header)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var found = element.TryGetProperty(header, out var property);

        if (!found)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Array => string.Join("|", property.EnumerateArray().Select(ScalarText)),
            _ => property.GetRawText(),
        };
    }

    private static string ScalarText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        return element.GetRawText();
    }

    private sealed class HeaderAccumulator
    {
        private readonly HashSet<string> Seen = new(StringComparer.Ordinal);

        private int Scanned;

        public List<string> Headers { get; } = [];

        public bool IsFull => Scanned >= HeaderScanLimit;

        public void Add(JsonElement element)
        {
            Scanned++;

            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (Seen.Add(property.Name))
                {
                    Headers.Add(property.Name);
                }
            }
        }
    }
}
