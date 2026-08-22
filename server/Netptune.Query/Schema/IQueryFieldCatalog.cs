namespace Netptune.Query.Schema;

// An interface rather than a fixed list so an entity becomes queryable by supplying a catalog alone.
public interface IQueryFieldCatalog
{
    IReadOnlyList<QueryField> Fields { get; }

    QueryField? Find(string? key);
}
