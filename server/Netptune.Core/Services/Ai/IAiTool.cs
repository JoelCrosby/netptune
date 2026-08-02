using System.Text.Json;

using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public enum AiToolKind
{
    Read = 0,
    Write = 1,
}

public sealed record AiToolExecution
{
    public required string Content { get; init; }

    public bool IsError { get; init; }

    public bool Truncated { get; init; }

    public static AiToolExecution Success(string content, bool truncated = false)
    {
        return new AiToolExecution { Content = content, Truncated = truncated };
    }

    public static AiToolExecution Failed(string message)
    {
        return new AiToolExecution { Content = message, IsError = true };
    }
}

public interface IAiTool
{
    string Name { get; }

    string Description { get; }

    AiToolKind Kind { get; }

    IReadOnlySet<string> RequiredPermissions { get; }

    JsonDocument InputSchema { get; }

    IReadOnlySet<string> GetRequiredPermissions(JsonElement payload)
    {
        return RequiredPermissions;
    }

    Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken);
}

public interface IAiToolRegistry
{
    IReadOnlyList<IAiTool> All { get; }

    IAiTool? Find(string name);

    IReadOnlyList<AiToolDefinition> GetDefinitions();
}
