using System.Collections.Generic;

namespace Netptune.Core.Requests;

public class MoveTasksToGroupRequest
{
    public string BoardId { get; set; }

    public List<int> TaskIds { get; set; }

    public int NewGroupId { get; set; }
}