namespace Netptune.App.Services;

public static class BoardEventServiceExtensions
{
    private const string RealtimeClientHeader = "X-Realtime-Client";
    private const string WorkspaceHeader = "workspace";

    // A broadcast that names no scope tells the client to refresh everything, so name what changed.
    public static Task BroadcastRequestAsync(
        this IBoardEventService service,
        HttpContext context,
        params string[] scopes)
    {
        var workspace = context.Request.Headers[WorkspaceHeader].ToString();

        if (string.IsNullOrWhiteSpace(workspace))
        {
            return Task.CompletedTask;
        }

        var requestedClientId = context.Request.Headers[RealtimeClientHeader].ToString();
        var sourceClientId = string.IsNullOrWhiteSpace(requestedClientId)
            ? context.Connection.Id
            : requestedClientId;

        return service.BroadcastAsync(workspace, sourceClientId, scopes);
    }
}
