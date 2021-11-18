using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public class ChangePasswordRequest
{
    [Required]
    public string UserId { get; set; }

    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword{ get; set; }
}