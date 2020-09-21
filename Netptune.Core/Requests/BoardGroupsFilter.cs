using System.Collections.Generic;

namespace Netptune.Core.Requests
{
    public class BoardGroupsFilter
    {
        public List<string> Users { get; set; }

        public List<string> Tags { get; set; }

        public bool Flagged { get; set; }
    }
}
