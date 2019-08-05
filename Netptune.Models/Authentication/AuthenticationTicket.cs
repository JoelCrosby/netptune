using System;

namespace Netptune.Entities.Authentication
{
    public struct AuthenticationTicket
    {
        public object Token;

        public string UserId;

        public string Emailaddress;

        public string DisplayName;

        public DateTime Issued;

        public DateTime Expires;
    }
}
