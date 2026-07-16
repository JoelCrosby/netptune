namespace Netptune.Core.Onboarding.Templates;

public sealed record CreateBoardGroupOptions
{
    public required string Name { get; init; }

    public double SortOrder { get; init; }

    public int? StatusId { get; init; }
}
