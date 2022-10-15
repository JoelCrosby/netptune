using System.Linq;

namespace Netptune.Core.Utilities;

public static class ConnectionStringParser
{
    public static string ParseConnectionString(string value, string? databaseName = null)
    {
        var conn = value
            .Replace("//", "")
            .Split('/', ':', '@', '?')
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var user = conn[1];
        var pass = conn[2];
        var server = conn[3];
        var database = databaseName ?? conn[5];
        var port = conn[4];

        return $"host={server};port={port};database={database};uid={user};pwd={pass};Timeout=1000";
    }

    public static string ParseRedis(string value)
    {
        if (value is "localhost") return value;

        var conn = value
            .Replace("//", "")
            .Split('/', ':', '@', '?')
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var pass = conn[2];
        var host = conn[3];
        var port = conn[4];

        return $"{host}:{port},password={pass}";
    }
}
