
using System.Collections.Generic;

namespace Netptune.Core.Responses;

public class InviteUserResponse
{
    public List<string> Emails { get; init; } = new();
}
