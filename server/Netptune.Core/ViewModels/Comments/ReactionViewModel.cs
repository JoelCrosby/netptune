namespace Netptune.Core.ViewModels.Comments;

public record ReactionViewModel
{
    public string Value { get; init; } = null!;

    public string UserId { get; init; } = null!;
}
