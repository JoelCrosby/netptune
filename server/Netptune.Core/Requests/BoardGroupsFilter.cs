namespace Netptune.Core.Requests;

public record BoardGroupsFilter
{
    public required string[] Users { get; init; }

    public required string[] Tags { get; init; }

    public bool? Flagged { get; init; }

    public string? Term { get; init; }

    public static BoardGroupsFilter Empty()
    {
        return new BoardGroupsFilter
        {
            Tags = [], Users = [],
        };
    }
}
