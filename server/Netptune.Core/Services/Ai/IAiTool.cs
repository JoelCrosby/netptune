using System.Text.Json;

using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public enum AiToolKind
{
    Read = 0,
    Write = 1,
    Question = 2,
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

    // A tool that proposes several kinds of change records each one under its own name, so handlers,
    // stored change sets and the client keep keying on the kind rather than on the tool that proposed it.
    IReadOnlyList<string> ProposedChanges => Kind == AiToolKind.Write ? [Name] : [];

    bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return RequiredPermissions.All(permissions.Contains);
    }

    IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        return RequiredPermissions;
    }

    IReadOnlySet<string> GetChangePermissions(string changeName, JsonElement payload)
    {
        return GetRequiredPermissions(payload);
    }

    Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken);
}

public interface IAiToolRegistry
{
    IReadOnlyList<IAiTool> All { get; }

    IAiTool? Find(string name);

    IAiTool? FindProposer(string changeName);

    IReadOnlyList<AiToolDefinition> GetDefinitions();
}
