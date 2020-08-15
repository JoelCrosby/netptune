using System;

namespace Netptune.Core.Authentication.Models
{
    public class AuthenticationTicket
    {
        public object Token { get; set; }

        public string UserId { get; set; }

        public string EmailAddress { get; set; }

        public string DisplayName { get; set; }

        public string PictureUrl { get; set; }

        public DateTime Issued { get; set; }

        public DateTime Expires { get; set; }
    }
}
