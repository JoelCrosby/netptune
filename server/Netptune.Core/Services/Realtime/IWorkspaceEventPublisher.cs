namespace Netptune.Core.Services.Realtime;

public interface IWorkspaceEventPublisher
{
    const string ChannelName = "workspace-events";

    Task PublishAsync(string workspace, string sourceClientId, string[] scopes);
}
