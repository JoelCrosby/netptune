using System.Text.Json;
using System.Text.Json.Nodes;

using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiResolvedFieldEdit
{
    public required string Name { get; init; }

    public string? After { get; init; }

    public List<AiChangeValue>? AfterValues { get; init; }

    public string? Summary { get; init; }

    public Dictionary<string, JsonNode?> Payload { get; init; } = [];

    public IReadOnlyList<string> RemovedPayload { get; init; } = [];
}

public sealed record AiChangeEditResult
{
    public string? Error { get; init; }

    public string Summary { get; init; } = string.Empty;

    public JsonDocument Fields { get; init; } = JsonDocument.Parse("[]");

    public JsonDocument Payload { get; init; } = JsonDocument.Parse("{}");

    public bool IsSuccess => Error is null;
}

// A proposal carries the same value twice: in Fields, which the reviewer reads, and in Payload,
// which the handler applies. Editing one without the other would apply something nobody reviewed.
public static class AiProposedChangeEditor
{
    private const int MaximumSummaryLength = 512;

    public static AiChangeEditResult Apply(
        string summary,
        JsonDocument fields,
        JsonDocument payload,
        IReadOnlyList<AiResolvedFieldEdit> edits)
    {
        if (edits.Count == 0)
        {
            return Failed("No field values were supplied.");
        }

        var current = AiChangeFieldSerializer.Deserialize(fields);
        var payloadNode = JsonNode.Parse(payload.RootElement.GetRawText());

        if (payloadNode is not JsonObject payloadObject)
        {
            return Failed("This change cannot be edited.");
        }

        var editedSummary = summary;

        foreach (var edit in edits)
        {
            var index = current.FindIndex(field => string.Equals(field.Name, edit.Name, StringComparison.Ordinal));

            if (index < 0)
            {
                return Failed($"“{edit.Name}” is not part of this change.");
            }

            var field = current[index];

            editedSummary = edit.Summary ?? Requote(editedSummary, field.After, edit.After);
            current[index] = field with { After = edit.After, AfterValues = edit.AfterValues };

            foreach (var (key, value) in edit.Payload)
            {
                payloadObject[key] = value;
            }

            foreach (var key in edit.RemovedPayload)
            {
                payloadObject.Remove(key);
            }
        }

        var writtenFields = SerializeFields(current);
        var writtenPayload = JsonDocument.Parse(payloadObject.ToJsonString());

        return new AiChangeEditResult
        {
            Summary = Truncate(editedSummary),
            Fields = writtenFields,
            Payload = writtenPayload,
        };
    }

    // A creation quotes the value it is about to write, so rewriting that value leaves the summary
    // lying. An update quotes the entity as it stands today, which the edit has not touched.
    private static string Requote(string summary, string? proposed, string? value)
    {
        var hasProposed = !string.IsNullOrWhiteSpace(proposed);
        var hasValue = !string.IsNullOrWhiteSpace(value);
        var quotesProposed = hasProposed && hasValue && summary.Contains($"“{proposed}”", StringComparison.Ordinal);

        if (!quotesProposed)
        {
            return summary;
        }

        return summary.Replace($"“{proposed}”", $"“{value}”", StringComparison.Ordinal);
    }

    private static string Truncate(string summary)
    {
        if (summary.Length <= MaximumSummaryLength)
        {
            return summary;
        }

        return summary[..MaximumSummaryLength];
    }

    private static JsonDocument SerializeFields(List<AiChangeFieldViewModel> fields)
    {
        var written = fields
            .Select(field => new AiChangeField
            {
                Name = field.Name,
                Before = field.Before,
                After = field.After,
                Kind = field.Kind,
                BeforeValues = field.BeforeValues,
                AfterValues = field.AfterValues,
            })
            .ToList();

        return AiChangeFieldSerializer.Serialize(written);
    }

    private static AiChangeEditResult Failed(string error)
    {
        return new AiChangeEditResult { Error = error };
    }
}
