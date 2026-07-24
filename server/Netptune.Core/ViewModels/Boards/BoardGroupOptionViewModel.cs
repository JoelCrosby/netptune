namespace Netptune.Core.ViewModels.Boards;

public sealed record BoardGroupOptionViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string BoardName { get; init; }

    public required string ProjectName { get; init; }
}
