namespace Netptune.Core.BaseEntities;

public abstract record KeyedEntity<TValue> : IKeyedEntity<TValue>
{
    public TValue Id { get; set; } = default!;
}
