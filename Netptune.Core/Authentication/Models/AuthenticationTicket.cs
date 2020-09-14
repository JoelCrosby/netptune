using System;

namespace Netptune.Core.Authentication.Models
{
    public class AuthenticationTicket : CurrentUserResponse
    {
        public object Token { get; set; }

        public DateTime Issued { get; set; }

        public DateTime Expires { get; set; }
    }
}
