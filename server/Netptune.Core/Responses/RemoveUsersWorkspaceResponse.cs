using System.Collections.Generic;

namespace Netptune.Core.Responses;

public class RemoveUsersWorkspaceResponse
{
    public List<string> Emails { get; init; } = new();
}
