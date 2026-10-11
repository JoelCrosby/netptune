using Netptune.Transfer;

namespace Netptune.Import.Archive;

// The ref → new id map that makes an archive portable. Built as each section lands, so a later
// section resolves its references without ever seeing an id from the system that wrote the archive.
public sealed class ArchiveImportContext
{
    private readonly Dictionary<string, int> IdsByRef = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> UserIdsByRef = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> Created = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> CreatedIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Type, int SourceId, Func<int> NewId)> Sources = [];

    public ArchiveImportContext(int workspaceId, string userId)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
    }

    public int WorkspaceId { get; }

    public string UserId { get; }

    public IReadOnlyDictionary<string, int> CreatedByType => Created;

    public IReadOnlyList<int> CreatedIdsOf(string type)
    {
        return CreatedIds.GetValueOrDefault(type) ?? [];
    }

    public void Register(EntityRef entityRef, int id)
    {
        if (id > 0)
        {
            IdsByRef[entityRef.ToString()] = id;
            AddCreatedId(entityRef.Type, id);
        }

        Count(entityRef.Type);
    }

    private void AddCreatedId(string type, int id)
    {
        if (!CreatedIds.TryGetValue(type, out var ids))
        {
            ids = [];
            CreatedIds[type] = ids;
        }

        ids.Add(id);
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

    public void TrackSource(string type, int? sourceId, Func<int> newId)
    {
        if (sourceId is { } id)
        {
            Sources.Add((type, id, newId));
        }
    }

    // Read once every tracked entity has been saved, so each new id is real.
    public Dictionary<string, Dictionary<int, int>> NewIdsBySourceId()
    {
        return Sources
            .GroupBy(source => source.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(source => source.SourceId, source => source.NewId()),
                StringComparer.OrdinalIgnoreCase);
    }

    public void Count(string type)
    {
        Created[type] = Created.GetValueOrDefault(type) + 1;
    }
}
