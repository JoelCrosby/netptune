namespace Netptune.Core.BaseEntities;

public abstract class KeyedEntity<TValue> : IKeyedEntity<TValue>
{
    public TValue Id { get; set; } = default!;
}
