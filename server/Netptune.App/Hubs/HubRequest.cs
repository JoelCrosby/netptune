namespace Netptune.App.Hubs;

public class HubRequest
{
    public string Group { get; set; } = null!;
}

public class HubRequest<TPayload> : HubRequest
{
    public string WorkspaceKey { get; set; } = null!;

    public TPayload? Payload { get; set; }
}
