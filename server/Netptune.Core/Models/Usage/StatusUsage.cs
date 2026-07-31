namespace Netptune.Core.Models.Usage;

public sealed record StatusUsage
{
    public int TaskCount { get; init; }

    public List<UsageReference> Projects { get; init; } = [];

    public List<UsageReference> BoardGroups { get; init; } = [];
}
