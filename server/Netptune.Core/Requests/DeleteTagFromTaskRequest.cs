namespace Netptune.Core.Requests
{
    public class DeleteTagFromTaskRequest
    {
        public string Workspace { get; set; }

        public string SystemId { get; set; }

        public string Tag { get; set; }
    }
}
