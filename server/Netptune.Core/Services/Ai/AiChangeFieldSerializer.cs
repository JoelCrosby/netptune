using System.Text.Json;

using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Services.Ai;

public static class AiChangeFieldSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static JsonDocument Serialize(List<AiChangeField> fields)
    {
        return JsonSerializer.SerializeToDocument(fields, Options);
    }

    public static List<AiChangeFieldViewModel> Deserialize(JsonDocument fields)
    {
        return fields.Deserialize<List<AiChangeFieldViewModel>>(Options) ?? [];
    }
}
