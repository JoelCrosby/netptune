using System.ComponentModel.DataAnnotations;

namespace Netptune.Services.Authentication
{
    public class NetptuneAuthenticationOptions
    {
        [Required]
        public string Issuer { get; set; }

        [Required]
        public string Audience { get; set; }

        [Required]
        public string SecurityKey { get; set; }

        [Required]
        public string GitHubClientId { get; set; }

        [Required]
        public string GitHubSecret { get; set; }
    }
}
