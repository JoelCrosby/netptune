using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;

namespace Netptune.Core.Repositories;

public sealed record SourceControlAccountKey
{
    public int WorkspaceId { get; init; }

    public SourceControlProvider Provider { get; init; }

    public string? BaseUrl { get; init; }

    public required string ExternalAccountId { get; init; }
}

public interface ISourceControlConnectionRepository : IRepository<SourceControlConnection, int>
{
    Task<List<SourceControlConnection>> GetForWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<SourceControlConnection?> GetOwned(int connectionId, int workspaceId, CancellationToken cancellationToken = default);

    Task<SourceControlConnection?> GetByAccount(SourceControlAccountKey key, CancellationToken cancellationToken = default);

    Task<List<SourceControlConnection>> GetByExternalAccount(SourceControlProvider provider, string externalAccountId, CancellationToken cancellationToken = default);
}
