using Netptune.Transfer;

namespace Netptune.Import.Archive;

// The ref → new id map that makes an archive portable. Built as each section lands, so a later
// section resolves its references without ever seeing an id from the system that wrote the archive.
public sealed class ArchiveImportContext
{
    private readonly Dictionary<string, int> IdsByRef = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> UserIdsByRef = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> Created = new(StringComparer.OrdinalIgnoreCase);

    public ArchiveImportContext(int workspaceId, string userId)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
    }

    public int WorkspaceId { get; }

    public string UserId { get; }

    public IReadOnlyDictionary<string, int> CreatedByType => Created;

    public void Register(EntityRef entityRef, int id)
    {
        if (id > 0)
        {
            IdsByRef[entityRef.ToString()] = id;
        }

        Count(entityRef.Type);
    }

    public void RegisterUser(EntityRef entityRef, string userId)
    {
        UserIdsByRef[entityRef.ToString()] = userId;
    }

    public int? Resolve(EntityRef? entityRef)
    {
        if (entityRef is null)
        {
            return null;
        }

        var found = IdsByRef.TryGetValue(entityRef.Value.ToString(), out var id);

        return found ? id : null;
    }

    public string? ResolveUser(EntityRef? entityRef)
    {
        if (entityRef is null)
        {
            return null;
        }

        var found = UserIdsByRef.TryGetValue(entityRef.Value.ToString(), out var userId);

        return found ? userId : null;
    }

    public void Count(string type)
    {
        Created[type] = Created.GetValueOrDefault(type) + 1;
    }
}
