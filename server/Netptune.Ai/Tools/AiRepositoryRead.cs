using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Authorization;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

internal static class AiRepositoryRead
{
    public const string RepositoryArgument = "repository";

    public const string RepositoryProperty =
        """
        "repository": { "type": "string", "description": "The repository's full name, owner/name, from list_repositories." }
        """;

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IReadOnlySet<string> Permissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.SourceControl.Read };

    public static string? Repository(JsonElement arguments)
    {
        var repository = AiToolSchema.GetString(arguments, RepositoryArgument)?.Trim();

        return string.IsNullOrWhiteSpace(repository) ? null : repository;
    }

    public static AiToolExecution MissingRepository()
    {
        return AiToolExecution.Failed("A repository is required, as owner/name. Call list_repositories to find one.");
    }

    public static AiToolExecution Failed<T>(ClientResponse<T> response)
    {
        return AiToolExecution.Failed(response.Message ?? "The repository could not be read.");
    }
}
