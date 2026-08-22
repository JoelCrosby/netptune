namespace Netptune.Query.Views;

public sealed record TaskViewColumn
{
    public required string Id { get; init; }

    public bool Visible { get; init; } = true;
}
