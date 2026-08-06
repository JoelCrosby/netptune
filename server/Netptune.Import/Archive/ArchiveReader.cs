using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Transfer;
using Netptune.Transfer.Archive;

namespace Netptune.Import.Archive;

public sealed record ArchiveRow
{
    public required EntityRef Ref { get; init; }

    public required JsonElement Values { get; init; }

    public string? Text(string name)
    {
        var found = Values.TryGetProperty(name, out var property);

        if (!found || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText();
    }

    public EntityRef? Reference(string name)
    {
        var raw = Text(name);
        var parsed = EntityRef.TryParse(raw, out var reference);

        return parsed ? reference : null;
    }

    public IReadOnlyList<EntityRef> References(string name)
    {
        var found = Values.TryGetProperty(name, out var property);

        if (!found || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Select(element => element.GetString())
            .Select(value => EntityRef.TryParse(value, out var reference) ? reference : (EntityRef?)null)
            .Where(reference => reference is not null)
            .Select(reference => reference!.Value)
            .ToList();
    }

    public bool Flag(string name)
    {
        var found = Values.TryGetProperty(name, out var property);

        return found && property.ValueKind == JsonValueKind.True;
    }

    public int? Integer(string name)
    {
        var found = Values.TryGetProperty(name, out var property);

        if (!found || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.GetInt32();
    }

    public double? Number(string name)
    {
        var found = Values.TryGetProperty(name, out var property);

        if (!found || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.GetDouble();
    }

    public decimal? Decimal(string name)
    {
        var value = Number(name);

        return value is null ? null : (decimal)value.Value;
    }

    // An archive always writes UTC. Parsing without these styles would read a trailing Z as local time
    // and the SpecifyKind below would then relabel the shifted value as UTC, moving every timestamp by
    // the host's offset on round-trip.
    public DateTime? Timestamp(string name)
    {
        var raw = Text(name);
        var parsed = DateTime.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var value);

        return parsed ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : null;
    }

    public DateOnly? Date(string name)
    {
        var raw = Text(name);
        var parsed = DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value);

        return parsed ? value : null;
    }

    public TEnum? Enum<TEnum>(string name) where TEnum : struct, Enum
    {
        var raw = Text(name);
        var parsed = System.Enum.TryParse<TEnum>(raw, true, out var value);

        return parsed ? value : null;
    }
}

// Reads a .nptz archive: the manifest first, then any declared section on demand.
public sealed class ArchiveReader : IDisposable
{
    private readonly ZipArchive Zip;

    public ArchiveReader(Stream source)
    {
        source.Seek(0, SeekOrigin.Begin);

        try
        {
            Zip = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException exception)
        {
            // An upload that is not a zip at all is a bad request, not a server fault.
            throw new ArchiveSchemaException("This file is not a Netptune archive: it could not be read as an archive.", exception);
        }
    }

    public ArchiveManifest ReadManifest()
    {
        var entry = Zip.GetEntry(ArchiveManifest.FileName)
            ?? throw new ArchiveSchemaException("This file is not a Netptune archive: it has no manifest.");

        using var stream = entry.Open();

        return JsonSerializer.Deserialize<ArchiveManifest>(stream, JsonOptions.Default)
            ?? throw new ArchiveSchemaException("The archive manifest could not be read.");
    }

    public bool HasSection(string fileName)
    {
        return Zip.GetEntry(fileName) is not null;
    }

    public async IAsyncEnumerable<ArchiveRow> ReadSection(
        string fileName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entry = Zip.GetEntry(fileName);

        if (entry is null)
        {
            yield break;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement.Clone();
            var parsed = EntityRef.TryParse(root.TryGetProperty("ref", out var reference) ? reference.GetString() : null, out var entityRef);

            if (!parsed)
            {
                continue;
            }

            yield return new ArchiveRow { Ref = entityRef, Values = root };
        }
    }

    public Stream? OpenFile(string entryPath)
    {
        return Zip.GetEntry(entryPath)?.Open();
    }

    public void Dispose()
    {
        Zip.Dispose();
    }
}
