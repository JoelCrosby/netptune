namespace Netptune.Core.Services.Realtime;

public sealed record WorkspaceEvent
{
    public required string Workspace { get; init; }

    public required string SourceClientId { get; init; }

    public string[] Scopes { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}
