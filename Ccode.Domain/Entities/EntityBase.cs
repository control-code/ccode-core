#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.


namespace Ccode.Domain.Entities
{
    public abstract class EntityBase : IEntityBase
    {
        internal Tracker Tracker { get; }

        public Guid Id { get; }

        public abstract object StateObject { get; }

        protected EntityBase(Guid id, Tracker tracker)
        {
            Id = id;
            Tracker = tracker;
        }
    }
}
