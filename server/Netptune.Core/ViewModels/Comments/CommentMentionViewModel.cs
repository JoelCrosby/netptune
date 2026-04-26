namespace Netptune.Core.ViewModels.Comments;

public record CommentMentionViewModel
{
    public string UserId { get; init; } = null!;

    public string DisplayName { get; init; } = null!;
}
