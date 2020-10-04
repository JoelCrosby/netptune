namespace Netptune.Core.Requests
{
    public class MoveTaskInGroupRequest
    {
        public int TaskId { get; set; }

        public int NewGroupId { get; set; }

        public int OldGroupId { get; set; }

        public double? SortOrder { get; set; }

        public int PreviousIndex { get; set; }

        public int CurrentIndex { get; set; }
    }
}
