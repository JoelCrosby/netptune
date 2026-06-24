using Netptune.Core.Authorization;

namespace Netptune.Repositories.RowMaps;

// Maps a row of get_workspace_users_paged.sql. Column aliases are underscore-free
// lowercase so Dapper's case-insensitive matching binds them to these properties.
public sealed class WorkspaceUserRowMap
{
    public string? Id { get; set; }

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public string? PictureUrl { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? Email { get; set; }

    public string? UserName { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public WorkspaceRole Role { get; set; }

    public bool IsPending { get; set; }

    public int TotalCount { get; set; }
}
