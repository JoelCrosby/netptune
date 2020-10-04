namespace Netptune.Models.BaseEntities
{
    public abstract class KeyedEntity<TValue>
    {
        public TValue Id { get; set; }
    }
}
