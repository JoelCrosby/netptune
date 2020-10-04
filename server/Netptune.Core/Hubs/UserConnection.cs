using Netptune.Core.Entities;

namespace Netptune.Core.Hubs
{
    public class UserConnection
    {
        public string ConnectId { get; set; }

        public AppUser User { get; set; }

        public string UserId { get; set; }
    }
}
