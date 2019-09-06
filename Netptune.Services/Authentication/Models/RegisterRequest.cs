namespace Netptune.Services.Authentication.Models
{
    public class RegisterRequest : TokenRequest
    {
        public string Firstname { get; set; }

        public string Lastname { get; set; }
    }
}
