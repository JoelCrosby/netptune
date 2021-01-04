namespace Netptune.Core.Requests
{
    public class UpdateBoardGroupRequest
    {
        public int BoardGroupId { get; set; }

        public string Name { get; set; }

        public double? SortOrder { get; set; }
    }
}
