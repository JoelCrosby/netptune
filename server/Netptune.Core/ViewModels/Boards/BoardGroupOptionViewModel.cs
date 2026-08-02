namespace Netptune.Core.ViewModels.Boards;

public sealed record BoardGroupOptionViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required int BoardId { get; init; }

    public required string BoardName { get; init; }

    public required string BoardIdentifier { get; init; }

    public required int ProjectId { get; init; }

    public required string ProjectName { get; init; }
}
