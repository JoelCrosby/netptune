using System.Text.Json;

namespace Netptune.Core.Encoding;

public static class JsonOptions
{
    public static JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}