namespace Netptune.Core.Models.Usage;

public sealed record UsageReference
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Context { get; init; }
}
