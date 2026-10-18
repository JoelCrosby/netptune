namespace Netptune.Core.Services.Realtime;

public interface IWorkspaceRefreshQueue
{
    void Enqueue(int workspaceId, IReadOnlyCollection<string> scopes);
}
