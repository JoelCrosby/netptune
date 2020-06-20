using System;

namespace Netptune.Core.ViewModels.Users
{
    public class UserViewModel
    {
        public string Id { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string PictureUrl { get; set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }

        public DateTimeOffset LastLoginTime { get; set; }

        public DateTimeOffset RegistrationDate { get; set; }
    }
}
