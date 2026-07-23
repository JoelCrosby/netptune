namespace Netptune.Core.Models.Automations;

public sealed record AutomationExecutionPrincipal
{
    public required string UserId { get; init; }

    public bool IsEnabled { get; init; }

    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>();
}
