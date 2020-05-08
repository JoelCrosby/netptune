namespace Netptune.Core.BaseEntities
{
    public abstract class KeyedEntity<TValue>
    {
        public TValue Id { get; set; }
    }
}
