using System.Text.Json;
using System.Text.Json.Serialization;

namespace Netptune.Core.SourceControl.Checks;

public static class ScmConnectionChecks
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static bool HasFailure(IEnumerable<ScmConnectionCheck> checks)
    {
        return checks.Any(check => check.Status == ScmConnectionCheckStatus.Failed);
    }

    public static bool HasWarning(IEnumerable<ScmConnectionCheck> checks)
    {
        return checks.Any(check => check.Status == ScmConnectionCheckStatus.Warning);
    }

    public static JsonDocument ToJson(IReadOnlyList<ScmConnectionCheck> checks)
    {
        return JsonSerializer.SerializeToDocument(checks, JsonOptions);
    }

    public static IReadOnlyList<ScmConnectionCheck> FromJson(JsonDocument? json)
    {
        if (json is null)
        {
            return [];
        }

        var checks = json.Deserialize<List<ScmConnectionCheck>>(JsonOptions);

        return checks ?? [];
    }
}
