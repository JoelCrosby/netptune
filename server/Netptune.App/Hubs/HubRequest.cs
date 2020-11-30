namespace Netptune.App.Hubs
{
    public class HubRequest
    {
        public string Group { get; set; }
    }

    public class HubRequest<TPayload> : HubRequest
    {
        public string WorkspaceKey { get; set; }

        public TPayload Payload { get; set; }
    }
}
