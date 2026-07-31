namespace Netptune.Core.ViewModels.Users;

public sealed class UserSelectOptionViewModel
{
    public string Id { get; init; } = null!;

    public string DisplayName { get; init; } = null!;

    public string? Email { get; init; }

    public string? PictureUrl { get; init; }

    public bool IsServiceAccount { get; init; }
}
