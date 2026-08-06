using Netptune.Transfer.Enums;
using System.Text.Json;

using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Records;

namespace Netptune.Export.Writers;

public sealed class JsonExportWriter : IExportWriter
{
    public JsonExportWriter(ExportFormat format)
    {
        Format = format;
    }

    public ExportFormat Format { get; }

    public string ContentType => Format == ExportFormat.Ndjson ? "application/x-ndjson" : "application/json";

    public string FileExtension => Format == ExportFormat.Ndjson ? "ndjson" : "json";

    public async Task<long> Write(ExportWriteRequest request, Stream output, CancellationToken cancellationToken = default)
    {
        var formatter = new ExportValueFormatter(request.Options);
        var isNdjson = Format == ExportFormat.Ndjson;

        await using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { SkipValidation = true });

        if (!isNdjson)
        {
            writer.WriteStartArray();
        }

        var rowCount = 0L;

        await foreach (var record in request.Records.WithCancellation(cancellationToken))
        {
            WriteRecord(writer, record, request.Fields, formatter);

            rowCount++;

            if (!isNdjson)
            {
                continue;
            }

            await writer.FlushAsync(cancellationToken);
            output.WriteByte((byte)'\n');
            writer.Reset(output);
        }

        if (!isNdjson)
        {
            writer.WriteEndArray();
        }

        await writer.FlushAsync(cancellationToken);

        return rowCount;
    }

    private static void WriteRecord(
        Utf8JsonWriter writer,
        ExportRecord record,
        IReadOnlyList<TransferField> fields,
        ExportValueFormatter formatter)
    {
        writer.WriteStartObject();
        writer.WriteString("ref", record.Ref.ToString());

        foreach (var field in fields)
        {
            writer.WritePropertyName(PropertyName(field));
            WriteValue(writer, record.Values.GetValueOrDefault(field.Key), formatter);
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value, ExportValueFormatter formatter)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;
            case EntityRef entityRef:
                writer.WriteStringValue(entityRef.ToString());
                return;
            case IEnumerable<EntityRef> refs:
                writer.WriteStartArray();

                foreach (var item in refs)
                {
                    writer.WriteStringValue(item.ToString());
                }

                writer.WriteEndArray();
                return;
            case bool flag:
                writer.WriteBooleanValue(flag);
                return;
            case decimal number:
                writer.WriteNumberValue(number);
                return;
            case int number:
                writer.WriteNumberValue(number);
                return;
            default:
                writer.WriteStringValue(formatter.Format(value));
                return;
        }
    }

    private static string PropertyName(TransferField field)
    {
        var separatorIndex = field.Key.IndexOf('.');

        if (separatorIndex < 0)
        {
            return field.Key;
        }

        return field.Key[(separatorIndex + 1)..];
    }
}
