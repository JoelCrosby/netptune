namespace Netptune.Core.BaseEntities
{
    public interface IKeyedEntity<TValue>
    {
        TValue Id { get; set; }
    }
}
