using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class ResetPasswordRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
