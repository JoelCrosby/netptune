namespace Netptune.Core.Requests
{
    public class AddCommentRequest
    {
        public string Comment { get; set; }

        public string SystemId { get; set; }

        public string WorkspaceSlug { get; set; }
    }
}
