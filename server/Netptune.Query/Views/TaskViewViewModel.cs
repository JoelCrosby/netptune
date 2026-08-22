
namespace Netptune.Query.Views;

public sealed record TaskViewViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string Slug { get; init; } = null!;

    public string? Icon { get; init; }

    public bool IsShared { get; init; }

    public TaskViewDefinition? Definition { get; init; }

    public string? CreatedByUserId { get; init; }

    public string? CreatedByDisplayName { get; init; }

    public bool IsOwn { get; init; }

    public bool CanEdit { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
