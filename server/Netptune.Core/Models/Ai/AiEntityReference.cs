using System.Text.Json;

namespace Netptune.Core.Models.Ai;

public sealed record AiEntityReference
{
    public required string Type { get; init; }

    public required string Id { get; init; }

    public required string Name { get; init; }

    // The segment the app routes this entity by: system id for tasks, key for projects and
    // identifier for boards and sprints.
    public required string RouteId { get; init; }
}

public static class AiEntityReferenceReader
{
    public const string Task = "task";
    public const string Project = "project";
    public const string Sprint = "sprint";
    public const string Board = "board";

    private const string ListRecordsTool = "list_records";

    // list_projects, list_sprints and list_boards were folded into list_records; they stay so the
    // conversations stored before that still link what they found.
    private static readonly Dictionary<string, string> TypesByTool = new(StringComparer.Ordinal)
    {
        ["search_tasks"] = Task,
        ["list_projects"] = Project,
        ["list_sprints"] = Sprint,
        ["list_boards"] = Board,
    };

    private static readonly Dictionary<string, string> TypesByRecordKind = new(StringComparer.Ordinal)
    {
        ["projects"] = Project,
        ["sprints"] = Sprint,
        ["boards"] = Board,
    };

    public static List<AiEntityReference> Read(IEnumerable<AiToolResultText> results)
    {
        var references = new List<AiEntityReference>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var result in results)
        {
            foreach (var reference in Read(result.ToolName, result.Content, result.Arguments))
            {
                var isNew = seen.Add($"{reference.Type}:{reference.Id}");

                if (isNew)
                {
                    references.Add(reference);
                }
            }
        }

        return references;
    }

    public static List<AiEntityReference> Read(string toolName, string? resultJson, JsonDocument? arguments = null)
    {
        var hasContent = !string.IsNullOrWhiteSpace(resultJson);
        var type = ResolveType(toolName, arguments);

        if (!hasContent || type is null)
        {
            return [];
        }

        var references = new List<AiEntityReference>();

        try
        {
            using var document = JsonDocument.Parse(resultJson!);

            Collect(document.RootElement, type, references);
        }
        catch (JsonException)
        {
            return [];
        }

        return references;
    }

    private static string? ResolveType(string toolName, JsonDocument? arguments)
    {
        var isListRecords = toolName == ListRecordsTool;

        if (!isListRecords)
        {
            return TypesByTool.GetValueOrDefault(toolName);
        }

        var isObject = arguments?.RootElement.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return null;
        }

        var hasKind = arguments!.RootElement.TryGetProperty("kind", out var kind)
            && kind.ValueKind == JsonValueKind.String;

        return hasKind ? TypesByRecordKind.GetValueOrDefault(kind.GetString()!) : null;
    }

    private static void Collect(JsonElement element, string type, List<AiEntityReference> references)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Collect(item, type, references);
            }

            return;
        }

        var isObject = element.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return;
        }

        var reference = CreateReference(element, type);

        if (reference is not null)
        {
            references.Add(reference);

            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            Collect(property.Value, type, references);
        }
    }

    private static AiEntityReference? CreateReference(JsonElement element, string type)
    {
        var hasName = element.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String;

        if (!hasName)
        {
            return null;
        }

        var id = ReadIdentifier(element, type);
        var routeId = ReadRouteId(element, type, id);

        if (id is null || routeId is null)
        {
            return null;
        }

        return new AiEntityReference
        {
            Type = type,
            Id = id,
            Name = name.GetString()!,
            RouteId = routeId,
        };
    }

    private static string? ReadRouteId(JsonElement element, string type, string? id)
    {
        return type switch
        {
            Project => ReadString(element, "key"),
            Board or Sprint => ReadString(element, "identifier"),
            _ => id,
        };
    }

    private static string? ReadString(JsonElement element, string name)
    {
        var hasValue = element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String;

        return hasValue ? value.GetString() : null;
    }

    private static string? ReadIdentifier(JsonElement element, string type)
    {
        var isTask = type == Task;

        if (isTask)
        {
            var hasSystemId = element.TryGetProperty("systemId", out var systemId)
                && systemId.ValueKind == JsonValueKind.String;

            return hasSystemId ? systemId.GetString() : null;
        }

        var hasId = element.TryGetProperty("id", out var identifier)
            && identifier.ValueKind == JsonValueKind.Number;

        return hasId ? identifier.GetInt32().ToString() : null;
    }
}

public sealed record AiToolResultText
{
    public required string ToolName { get; init; }

    public required string Content { get; init; }

    public JsonDocument? Arguments { get; init; }
}
