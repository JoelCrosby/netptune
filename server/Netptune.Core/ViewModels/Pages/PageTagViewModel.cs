namespace Netptune.Core.ViewModels.Pages;

public sealed record PageTagViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }
}
