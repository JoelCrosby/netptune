using System.Collections.Generic;

namespace Netptune.Core.Requests
{
    public class ReassignTasksRequest
    {
        public string BoardId { get; set; }

        public List<int> TaskIds { get; set; }

        public string AssigneeId { get; set; }
    }
}
