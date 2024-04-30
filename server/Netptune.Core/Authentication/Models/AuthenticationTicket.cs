using System;

namespace Netptune.Core.Authentication.Models;

public class AuthenticationTicket : CurrentUserResponse
{
    public object Token { get; set; } = null!;

    public DateTime Issued { get; set; }

    public DateTime Expires { get; set; }

    public string? WorkspaceSlug { get; set; }
}
