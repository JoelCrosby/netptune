using Netptune.Core.Authorization;

namespace Netptune.Core.ViewModels.Users;

public class UserViewModel
{
    public string Id { get; init; } = null!;

    public string Firstname { get; init; } = null!;

    public string Lastname { get; init; } = null!;

    public string? PictureUrl { get; init; }

    public string DisplayName { get; init; } = null!;

    public string? Email { get; init; }

    public string? UserName { get; init; }

    public DateTime? LastLoginTime { get; init; }

    public DateTime? RegistrationDate { get; init; }

    public List<string> Permissions { get; init; }
}

public class WorkspaceUserViewModel : UserViewModel
{
    public WorkspaceRole Role { get; init; }
}
