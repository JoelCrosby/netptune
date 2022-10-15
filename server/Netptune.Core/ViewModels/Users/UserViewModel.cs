using System;

namespace Netptune.Core.ViewModels.Users;

public class UserViewModel
{
    public string Id { get; init; } = null!;

    public string Firstname { get; init; } = null!;

    public string Lastname { get; init; } = null!;

    public string? PictureUrl { get; init; }

    public string DisplayName { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string UserName { get; init; } = null!;

    public DateTime? LastLoginTime { get; init; }

    public DateTime? RegistrationDate { get; init; }
}

public class WorkspaceUserViewModel : UserViewModel
{
    public bool IsWorkspaceOwner { get; init; }
}
