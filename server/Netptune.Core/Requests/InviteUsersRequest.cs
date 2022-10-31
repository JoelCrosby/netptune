using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record InviteUsersRequest
{
    [Required]
    public List<string> EmailAddresses { get; set; } = null!;
}
