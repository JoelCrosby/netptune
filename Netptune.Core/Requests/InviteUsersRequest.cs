using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class InviteUsersRequest
    {
        [Required]
        public List<string> EmailAddresses { get; set; }

        [Required]
        public string WorkspaceSlug { get; set; }
    }
}
